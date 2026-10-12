using System.Globalization;
using System.Text.Json;
using FileOrganizer.Core.Analysis;
using FileOrganizer.Core.Models;
using FileOrganizer.Core.Rules;
using Microsoft.Data.Sqlite;

namespace FileOrganizer.Core.Data;

/// <summary>
/// Локальная база SQLite: настройки, правила, профили, исключения, хеши, история операций, теги.
/// Содержимое документов пользователя не хранится.
/// </summary>
public sealed class Database : IHashCache
{
    public const int SchemaVersion = 1;
    private readonly string _connectionString;
    private readonly object _hashLock = new();
    private readonly List<(string path, long size, long mtime, string hash)> _pendingHashes = new();

    public string Path { get; }

    public static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    public Database(string path)
    {
        Path = path;
        var dir = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadWriteCreate, Pooling = true }.ToString();
        Migrate();
    }

    private SqliteConnection Open()
    {
        var c = new SqliteConnection(_connectionString);
        c.Open();
        using var pragma = c.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON; PRAGMA busy_timeout = 5000;";
        pragma.ExecuteNonQuery();
        return c;
    }

    private void Migrate()
    {
        using var c = Open();
        using (var wal = c.CreateCommand()) { wal.CommandText = "PRAGMA journal_mode = WAL;"; wal.ExecuteNonQuery(); }
        using var cmd = c.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS schema_info (version INTEGER NOT NULL);
            CREATE TABLE IF NOT EXISTS settings (key TEXT PRIMARY KEY, value TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS profiles (
                id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL UNIQUE, is_active INTEGER NOT NULL DEFAULT 0,
                settings_json TEXT NOT NULL DEFAULT '{}', created_at TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS rules (
                id INTEGER PRIMARY KEY AUTOINCREMENT, profile_id INTEGER NOT NULL REFERENCES profiles(id) ON DELETE CASCADE,
                name TEXT NOT NULL, enabled INTEGER NOT NULL, priority INTEGER NOT NULL, rule_json TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS exclusions (
                id INTEGER PRIMARY KEY AUTOINCREMENT, profile_id INTEGER NOT NULL REFERENCES profiles(id) ON DELETE CASCADE,
                kind TEXT NOT NULL, pattern TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS hash_cache (
                path TEXT PRIMARY KEY, size INTEGER NOT NULL, mtime_ticks INTEGER NOT NULL, sha256 TEXT NOT NULL, updated_at TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS scans (
                id INTEGER PRIMARY KEY AUTOINCREMENT, root TEXT NOT NULL, started_at TEXT NOT NULL, finished_at TEXT,
                file_count INTEGER, folder_count INTEGER, total_size INTEGER, duplicate_count INTEGER, duplicate_bytes INTEGER,
                empty_folder_count INTEGER, error_count INTEGER, summary_json TEXT);
            CREATE TABLE IF NOT EXISTS batches (
                id INTEGER PRIMARY KEY AUTOINCREMENT, title TEXT NOT NULL, root TEXT NOT NULL, recovery_folder TEXT NOT NULL,
                created_at TEXT NOT NULL, status TEXT NOT NULL, op_count INTEGER NOT NULL DEFAULT 0);
            CREATE TABLE IF NOT EXISTS operations (
                id INTEGER PRIMARY KEY AUTOINCREMENT, batch_id INTEGER NOT NULL REFERENCES batches(id) ON DELETE CASCADE,
                seq INTEGER NOT NULL, kind TEXT NOT NULL, source TEXT NOT NULL, destination TEXT NOT NULL,
                old_name TEXT NOT NULL, new_name TEXT NOT NULL, status TEXT NOT NULL, error TEXT, reason TEXT,
                size INTEGER NOT NULL DEFAULT 0, executed_at TEXT);
            CREATE INDEX IF NOT EXISTS ix_operations_batch ON operations(batch_id, seq);
            CREATE TABLE IF NOT EXISTS tags (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL UNIQUE, color TEXT);
            CREATE TABLE IF NOT EXISTS file_tags (
                path TEXT NOT NULL, tag_id INTEGER NOT NULL REFERENCES tags(id) ON DELETE CASCADE, PRIMARY KEY(path, tag_id));
            CREATE TABLE IF NOT EXISTS watched_folders (
                id INTEGER PRIMARY KEY AUTOINCREMENT, path TEXT NOT NULL UNIQUE, enabled INTEGER NOT NULL DEFAULT 1, last_checked TEXT);
            """;
        cmd.ExecuteNonQuery();
        using var ver = c.CreateCommand();
        ver.CommandText = "SELECT COUNT(*) FROM schema_info";
        if (Convert.ToInt64(ver.ExecuteScalar()) == 0)
        {
            ver.CommandText = $"INSERT INTO schema_info(version) VALUES ({SchemaVersion})";
            ver.ExecuteNonQuery();
        }
        EnsureDefaultProfile(c);
    }

    private static string Now() => DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
    private static DateTime ParseDate(string s) => DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    private static SqliteCommand Cmd(SqliteConnection c, string sql, params (string, object?)[] args)
    {
        var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (n, v) in args) cmd.Parameters.AddWithValue(n, v ?? DBNull.Value);
        return cmd;
    }

    // ---------------- Настройки ----------------

    public string? GetSetting(string key)
    {
        using var c = Open();
        using var cmd = Cmd(c, "SELECT value FROM settings WHERE key=$k", ("$k", key));
        return cmd.ExecuteScalar() as string;
    }

    public void SetSetting(string key, string value)
    {
        using var c = Open();
        using var cmd = Cmd(c, "INSERT INTO settings(key,value) VALUES($k,$v) ON CONFLICT(key) DO UPDATE SET value=excluded.value", ("$k", key), ("$v", value));
        cmd.ExecuteNonQuery();
    }

    public AppSettings LoadSettings()
    {
        var json = GetSetting("app");
        if (json == null) return new AppSettings();
        try { return JsonSerializer.Deserialize<AppSettings>(json, Json) ?? new AppSettings(); }
        catch (JsonException) { return new AppSettings(); }
    }

    public void SaveSettings(AppSettings s) => SetSetting("app", JsonSerializer.Serialize(s, Json));

    // ---------------- Профили, правила, исключения ----------------

    private static void EnsureDefaultProfile(SqliteConnection c)
    {
        using var cnt = Cmd(c, "SELECT COUNT(*) FROM profiles");
        if (Convert.ToInt64(cnt.ExecuteScalar()) > 0) return;
        using var tx = c.BeginTransaction();
        foreach (var (name, active) in new[] { ("Загрузки", 1), ("Работа", 0), ("Личные файлы", 0), ("Фото", 0), ("Архив", 0) })
        {
            using var ins = Cmd(c, "INSERT INTO profiles(name,is_active,created_at) VALUES($n,$a,$t)", ("$n", name), ("$a", active), ("$t", Now()));
            ins.Transaction = tx;
            ins.ExecuteNonQuery();
        }
        using var idCmd = Cmd(c, "SELECT id FROM profiles WHERE name='Загрузки'");
        idCmd.Transaction = tx;
        var pid = Convert.ToInt64(idCmd.ExecuteScalar());
        foreach (var e in ExclusionSet.Default().Items)
        {
            using var ins = Cmd(c, "INSERT INTO exclusions(profile_id,kind,pattern) VALUES($p,$k,$v)", ("$p", pid), ("$k", e.Kind.ToString()), ("$v", e.Pattern));
            ins.Transaction = tx;
            ins.ExecuteNonQuery();
        }
        tx.Commit();
    }

    public List<Profile> GetProfiles()
    {
        using var c = Open();
        using var cmd = Cmd(c, "SELECT id,name,is_active FROM profiles ORDER BY id");
        using var r = cmd.ExecuteReader();
        var list = new List<Profile>();
        while (r.Read()) list.Add(new Profile { Id = r.GetInt64(0), Name = r.GetString(1), IsActive = r.GetInt64(2) == 1 });
        return list;
    }

    public Profile GetActiveProfile() => GetProfiles().FirstOrDefault(p => p.IsActive) ?? GetProfiles().First();

    public long CreateProfile(string name)
    {
        using var c = Open();
        using var cmd = Cmd(c, "INSERT INTO profiles(name,is_active,created_at) VALUES($n,0,$t) RETURNING id", ("$n", name), ("$t", Now()));
        return Convert.ToInt64(cmd.ExecuteScalar());
    }

    public void DeleteProfile(long id)
    {
        using var c = Open();
        using var cmd = Cmd(c, "DELETE FROM profiles WHERE id=$id AND (SELECT COUNT(*) FROM profiles) > 1", ("$id", id));
        cmd.ExecuteNonQuery();
        using var fix = Cmd(c, "UPDATE profiles SET is_active=1 WHERE id=(SELECT MIN(id) FROM profiles) AND NOT EXISTS (SELECT 1 FROM profiles WHERE is_active=1)");
        fix.ExecuteNonQuery();
    }

    public void SetActiveProfile(long id)
    {
        using var c = Open();
        using var cmd = Cmd(c, "UPDATE profiles SET is_active = CASE WHEN id=$id THEN 1 ELSE 0 END", ("$id", id));
        cmd.ExecuteNonQuery();
    }

    public List<Rule> GetRules(long profileId)
    {
        using var c = Open();
        using var cmd = Cmd(c, "SELECT id,rule_json FROM rules WHERE profile_id=$p ORDER BY priority,id", ("$p", profileId));
        using var r = cmd.ExecuteReader();
        var list = new List<Rule>();
        while (r.Read())
        {
            var rule = JsonSerializer.Deserialize<Rule>(r.GetString(1), Json);
            if (rule == null) continue;
            rule.Id = r.GetInt64(0);
            list.Add(rule);
        }
        return list;
    }

    public long SaveRule(long profileId, Rule rule)
    {
        using var c = Open();
        var json = JsonSerializer.Serialize(rule, Json);
        if (rule.Id == 0)
        {
            using var ins = Cmd(c, "INSERT INTO rules(profile_id,name,enabled,priority,rule_json) VALUES($p,$n,$e,$pr,$j) RETURNING id",
                ("$p", profileId), ("$n", rule.Name), ("$e", rule.Enabled ? 1 : 0), ("$pr", rule.Priority), ("$j", json));
            rule.Id = Convert.ToInt64(ins.ExecuteScalar());
        }
        else
        {
            using var up = Cmd(c, "UPDATE rules SET name=$n,enabled=$e,priority=$pr,rule_json=$j WHERE id=$id",
                ("$n", rule.Name), ("$e", rule.Enabled ? 1 : 0), ("$pr", rule.Priority), ("$j", json), ("$id", rule.Id));
            up.ExecuteNonQuery();
        }
        return rule.Id;
    }

    public void DeleteRule(long id)
    {
        using var c = Open();
        using var cmd = Cmd(c, "DELETE FROM rules WHERE id=$id", ("$id", id));
        cmd.ExecuteNonQuery();
    }

    public ExclusionSet GetExclusions(long profileId)
    {
        using var c = Open();
        using var cmd = Cmd(c, "SELECT kind,pattern FROM exclusions WHERE profile_id=$p ORDER BY id", ("$p", profileId));
        using var r = cmd.ExecuteReader();
        var set = new ExclusionSet();
        while (r.Read())
            if (Enum.TryParse<ExclusionKind>(r.GetString(0), out var k)) set.Items.Add(new Exclusion(k, r.GetString(1)));
        return set;
    }

    public void SaveExclusions(long profileId, ExclusionSet set)
    {
        using var c = Open();
        using var tx = c.BeginTransaction();
        using (var del = Cmd(c, "DELETE FROM exclusions WHERE profile_id=$p", ("$p", profileId))) { del.Transaction = tx; del.ExecuteNonQuery(); }
        foreach (var e in set.Items)
        {
            using var ins = Cmd(c, "INSERT INTO exclusions(profile_id,kind,pattern) VALUES($p,$k,$v)", ("$p", profileId), ("$k", e.Kind.ToString()), ("$v", e.Pattern));
            ins.Transaction = tx;
            ins.ExecuteNonQuery();
        }
        tx.Commit();
    }

    // ---------------- Кэш хешей ----------------

    private System.Collections.Concurrent.ConcurrentDictionary<string, (long size, long mtime, string hash)>? _hashMemory;

    /// <summary>Кэш хешей загружается в память один раз — без обращения к базе на каждый файл.</summary>
    private System.Collections.Concurrent.ConcurrentDictionary<string, (long size, long mtime, string hash)> HashMemory
    {
        get
        {
            if (_hashMemory != null) return _hashMemory;
            lock (_hashLock)
            {
                if (_hashMemory != null) return _hashMemory;
                var d = new System.Collections.Concurrent.ConcurrentDictionary<string, (long, long, string)>(StringComparer.Ordinal);
                using var c = Open();
                using var cmd = Cmd(c, "SELECT path,size,mtime_ticks,sha256 FROM hash_cache");
                using var r = cmd.ExecuteReader();
                while (r.Read()) d[r.GetString(0)] = (r.GetInt64(1), r.GetInt64(2), r.GetString(3));
                _hashMemory = d;
                return d;
            }
        }
    }

    public bool TryGet(string path, long size, DateTime mtimeUtc, out string sha256)
    {
        sha256 = "";
        if (HashMemory.TryGetValue(path, out var e) && e.size == size && e.mtime == mtimeUtc.Ticks)
        {
            sha256 = e.hash;
            return true;
        }
        return false;
    }

    public void Set(string path, long size, DateTime mtimeUtc, string sha256)
    {
        HashMemory[path] = (size, mtimeUtc.Ticks, sha256);
        lock (_hashLock)
        {
            _pendingHashes.Add((path, size, mtimeUtc.Ticks, sha256));
            if (_pendingHashes.Count < 2000) return;
        }
        Flush();
    }

    public void Flush()
    {
        List<(string path, long size, long mtime, string hash)> items;
        lock (_hashLock)
        {
            if (_pendingHashes.Count == 0) return;
            items = new(_pendingHashes);
            _pendingHashes.Clear();
        }
        using var c = Open();
        using var tx = c.BeginTransaction();
        using var cmd = Cmd(c, "INSERT INTO hash_cache(path,size,mtime_ticks,sha256,updated_at) VALUES($p,$s,$m,$h,$t) " +
                               "ON CONFLICT(path) DO UPDATE SET size=excluded.size, mtime_ticks=excluded.mtime_ticks, sha256=excluded.sha256, updated_at=excluded.updated_at",
            ("$p", ""), ("$s", 0L), ("$m", 0L), ("$h", ""), ("$t", Now()));
        cmd.Transaction = tx;
        foreach (var (path, size, mtime, hash) in items)
        {
            cmd.Parameters["$p"].Value = path;
            cmd.Parameters["$s"].Value = size;
            cmd.Parameters["$m"].Value = mtime;
            cmd.Parameters["$h"].Value = hash;
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }

    public void ClearHashCache()
    {
        using var c = Open();
        using var cmd = Cmd(c, "DELETE FROM hash_cache");
        cmd.ExecuteNonQuery();
        _hashMemory = null;
    }

    // ---------------- Результаты анализа ----------------

    public void SaveScanSummary(AnalysisReport r)
    {
        using var c = Open();
        using var cmd = Cmd(c, """
            INSERT INTO scans(root,started_at,finished_at,file_count,folder_count,total_size,duplicate_count,duplicate_bytes,empty_folder_count,error_count)
            VALUES($r,$s,$f,$fc,$dc,$ts,$dup,$db,$ef,$err)
            """,
            ("$r", r.Scan.RootPath), ("$s", r.Scan.StartedUtc.ToString("O")), ("$f", r.Scan.FinishedUtc.ToString("O")),
            ("$fc", r.FileCount), ("$dc", r.FolderCount), ("$ts", r.TotalSize), ("$dup", r.DuplicateFileCount), ("$db", r.DuplicateBytes),
            ("$ef", r.EmptyFolders.Count), ("$err", r.Scan.Errors.Count));
        cmd.ExecuteNonQuery();
    }

    public List<(string Root, DateTime Finished, int Files, long Size)> GetRecentScans(int limit = 20)
    {
        using var c = Open();
        using var cmd = Cmd(c, "SELECT root,finished_at,file_count,total_size FROM scans ORDER BY id DESC LIMIT $l", ("$l", limit));
        using var r = cmd.ExecuteReader();
        var list = new List<(string, DateTime, int, long)>();
        while (r.Read()) list.Add((r.GetString(0), ParseDate(r.GetString(1)), r.GetInt32(2), r.GetInt64(3)));
        return list;
    }

    // ---------------- Журнал операций ----------------

    public long CreateBatch(string title, string root, string recoveryFolder, int opCount)
    {
        using var c = Open();
        using var cmd = Cmd(c, "INSERT INTO batches(title,root,recovery_folder,created_at,status,op_count) VALUES($t,$r,$rf,$c,$s,$n) RETURNING id",
            ("$t", title), ("$r", root), ("$rf", recoveryFolder), ("$c", Now()), ("$s", BatchStatus.Running.ToString()), ("$n", opCount));
        return Convert.ToInt64(cmd.ExecuteScalar());
    }

    public void SetBatchStatus(long batchId, BatchStatus status)
    {
        using var c = Open();
        using var cmd = Cmd(c, "UPDATE batches SET status=$s WHERE id=$id", ("$s", status.ToString()), ("$id", batchId));
        cmd.ExecuteNonQuery();
    }

    /// <summary>Записывает операцию ДО изменения файловой системы (статус Pending) — для восстановления после сбоя.</summary>
    public long AddOperation(OperationRecord rec)
    {
        using var c = Open();
        using var cmd = Cmd(c, """
            INSERT INTO operations(batch_id,seq,kind,source,destination,old_name,new_name,status,error,reason,size,executed_at)
            VALUES($b,$seq,$k,$s,$d,$on,$nn,$st,$e,$r,$sz,$t) RETURNING id
            """,
            ("$b", rec.BatchId), ("$seq", rec.Seq), ("$k", rec.Kind.ToString()), ("$s", rec.Source), ("$d", rec.Destination),
            ("$on", rec.OldName), ("$nn", rec.NewName), ("$st", rec.Status.ToString()), ("$e", rec.Error), ("$r", rec.Reason),
            ("$sz", rec.Size), ("$t", rec.ExecutedUtc?.ToString("O")));
        rec.Id = Convert.ToInt64(cmd.ExecuteScalar());
        return rec.Id;
    }

    public void UpdateOperation(long id, OperationStatus status, string? error = null, string? destination = null)
    {
        using var c = Open();
        using var cmd = Cmd(c, """
            UPDATE operations SET status=$s, error=$e, executed_at=$t,
              destination=COALESCE($d, destination), new_name=COALESCE($nn, new_name) WHERE id=$id
            """,
            ("$s", status.ToString()), ("$e", error), ("$t", Now()), ("$d", destination),
            ("$nn", destination == null ? null : System.IO.Path.GetFileName(destination)), ("$id", id));
        cmd.ExecuteNonQuery();
    }

    public List<BatchInfo> GetBatches(int limit = 500)
    {
        using var c = Open();
        using var cmd = Cmd(c, """
            SELECT b.id,b.title,b.root,b.recovery_folder,b.created_at,b.status,b.op_count,
              (SELECT COUNT(*) FROM operations o WHERE o.batch_id=b.id AND o.status='Done'),
              (SELECT COUNT(*) FROM operations o WHERE o.batch_id=b.id AND o.status IN ('Failed','RollbackFailed'))
            FROM batches b ORDER BY b.id DESC LIMIT $l
            """, ("$l", limit));
        using var r = cmd.ExecuteReader();
        var list = new List<BatchInfo>();
        while (r.Read())
        {
            list.Add(new BatchInfo
            {
                Id = r.GetInt64(0), Title = r.GetString(1), RootPath = r.GetString(2), RecoveryFolder = r.GetString(3),
                CreatedUtc = ParseDate(r.GetString(4)), Status = Enum.Parse<BatchStatus>(r.GetString(5)), OperationCount = r.GetInt32(6),
                DoneCount = r.GetInt32(7), FailedCount = r.GetInt32(8),
            });
        }
        return list;
    }

    public BatchInfo? GetBatch(long id) => GetBatches(int.MaxValue).FirstOrDefault(b => b.Id == id);

    public List<OperationRecord> GetOperations(long batchId)
    {
        using var c = Open();
        using var cmd = Cmd(c, "SELECT id,batch_id,seq,kind,source,destination,status,error,reason,size,executed_at FROM operations WHERE batch_id=$b ORDER BY seq, id", ("$b", batchId));
        using var r = cmd.ExecuteReader();
        var list = new List<OperationRecord>();
        while (r.Read())
        {
            list.Add(new OperationRecord
            {
                Id = r.GetInt64(0), BatchId = r.GetInt64(1), Seq = r.GetInt32(2), Kind = Enum.Parse<OperationKind>(r.GetString(3)),
                Source = r.GetString(4), Destination = r.GetString(5), Status = Enum.Parse<OperationStatus>(r.GetString(6)),
                Error = r.IsDBNull(7) ? null : r.GetString(7), Reason = r.IsDBNull(8) ? null : r.GetString(8), Size = r.GetInt64(9),
                ExecutedUtc = r.IsDBNull(10) ? null : ParseDate(r.GetString(10)),
            });
        }
        return list;
    }

    // ---------------- Теги ----------------

    public List<(long Id, string Name, string? Color)> GetTags()
    {
        using var c = Open();
        using var cmd = Cmd(c, "SELECT id,name,color FROM tags ORDER BY name");
        using var r = cmd.ExecuteReader();
        var list = new List<(long, string, string?)>();
        while (r.Read()) list.Add((r.GetInt64(0), r.GetString(1), r.IsDBNull(2) ? null : r.GetString(2)));
        return list;
    }

    public long EnsureTag(string name, string? color = null)
    {
        using var c = Open();
        using var ins = Cmd(c, "INSERT INTO tags(name,color) VALUES($n,$c) ON CONFLICT(name) DO NOTHING", ("$n", name), ("$c", color));
        ins.ExecuteNonQuery();
        using var sel = Cmd(c, "SELECT id FROM tags WHERE name=$n", ("$n", name));
        return Convert.ToInt64(sel.ExecuteScalar());
    }

    public void TagFile(string path, string tag)
    {
        var id = EnsureTag(tag);
        using var c = Open();
        using var cmd = Cmd(c, "INSERT OR IGNORE INTO file_tags(path,tag_id) VALUES($p,$t)", ("$p", path), ("$t", id));
        cmd.ExecuteNonQuery();
    }

    public void UntagFile(string path, string tag)
    {
        using var c = Open();
        using var cmd = Cmd(c, "DELETE FROM file_tags WHERE path=$p AND tag_id=(SELECT id FROM tags WHERE name=$n)", ("$p", path), ("$n", tag));
        cmd.ExecuteNonQuery();
    }

    public Dictionary<string, List<string>> GetFileTags()
    {
        using var c = Open();
        using var cmd = Cmd(c, "SELECT ft.path, t.name FROM file_tags ft JOIN tags t ON t.id=ft.tag_id");
        using var r = cmd.ExecuteReader();
        var d = new Dictionary<string, List<string>>(Infrastructure.PathUtil.Comparer);
        while (r.Read())
        {
            if (!d.TryGetValue(r.GetString(0), out var l)) d[r.GetString(0)] = l = new List<string>();
            l.Add(r.GetString(1));
        }
        return d;
    }

    /// <summary>Теги «следуют» за файлом при перемещении/переименовании и откате.</summary>
    public void MoveFileTags(string from, string to)
    {
        using var c = Open();
        using var cmd = Cmd(c, "UPDATE OR IGNORE file_tags SET path=$to WHERE path=$from", ("$to", to), ("$from", from));
        cmd.ExecuteNonQuery();
    }

    // ---------------- Папки «Разобрать» (Inbox) ----------------

    public List<(long Id, string Path, bool Enabled, DateTime? LastChecked)> GetWatchedFolders()
    {
        using var c = Open();
        using var cmd = Cmd(c, "SELECT id,path,enabled,last_checked FROM watched_folders ORDER BY id");
        using var r = cmd.ExecuteReader();
        var list = new List<(long, string, bool, DateTime?)>();
        while (r.Read()) list.Add((r.GetInt64(0), r.GetString(1), r.GetInt64(2) == 1, r.IsDBNull(3) ? null : ParseDate(r.GetString(3))));
        return list;
    }

    public void AddWatchedFolder(string path)
    {
        using var c = Open();
        using var cmd = Cmd(c, "INSERT OR IGNORE INTO watched_folders(path,enabled) VALUES($p,1)", ("$p", path));
        cmd.ExecuteNonQuery();
    }

    public void RemoveWatchedFolder(long id)
    {
        using var c = Open();
        using var cmd = Cmd(c, "DELETE FROM watched_folders WHERE id=$id", ("$id", id));
        cmd.ExecuteNonQuery();
    }

    public void MarkWatchedFolderChecked(long id)
    {
        using var c = Open();
        using var cmd = Cmd(c, "UPDATE watched_folders SET last_checked=$t WHERE id=$id", ("$t", Now()), ("$id", id));
        cmd.ExecuteNonQuery();
    }
}

public sealed class Profile
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; }
    public override string ToString() => Name;
}
