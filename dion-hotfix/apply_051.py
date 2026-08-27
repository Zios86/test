from pathlib import Path
import sys

root = Path(sys.argv[1] if len(sys.argv) > 1 else 'src')

audio = root / 'app' / 'audio.py'
s = audio.read_text(encoding='utf-8')

s = s.replace(
    'import pyaudiowpatch as pyaudio\n\nfrom .health import SessionHealth\n',
    '''import pyaudiowpatch as pyaudio\n\n# Hotfix 0.5.1: keep one process-wide PortAudio interface.\n# Multiple concurrent PyAudio() init/terminate cycles from capture threads can\n# crash the native PortAudio DLL and close the whole GUI without a Python error.\n_PA_LOCK = threading.RLock()\n_PA_OPEN_LOCK = threading.RLock()\n_PA_INSTANCE = None\n_PA_REFS = 0\n\ndef _acquire_pa():\n    global _PA_INSTANCE, _PA_REFS\n    with _PA_LOCK:\n        if _PA_INSTANCE is None:\n            _PA_INSTANCE = pyaudio.PyAudio()\n        _PA_REFS += 1\n        return _PA_INSTANCE\n\ndef _release_pa() -> None:\n    global _PA_INSTANCE, _PA_REFS\n    with _PA_LOCK:\n        _PA_REFS = max(0, _PA_REFS - 1)\n        if _PA_REFS == 0 and _PA_INSTANCE is not None:\n            try:\n                _PA_INSTANCE.terminate()\n            finally:\n                _PA_INSTANCE = None\n\nfrom .health import SessionHealth\n'''
)

# Device enumeration/default-device lookups: use the shared interface.
s = s.replace(
    '        with pyaudio.PyAudio() as pa:\n            for info in pa.get_loopback_device_info_generator():\n',
    '        pa = _acquire_pa()\n        try:\n            for info in pa.get_loopback_device_info_generator():\n'
)
s = s.replace(
    '                        is_loopback=True,\n                    )\n                )\n        return devices\n',
    '                        is_loopback=True,\n                    )\n                )\n        finally:\n            _release_pa()\n        return devices\n',
    1,
)
s = s.replace(
    '        with pyaudio.PyAudio() as pa:\n            for info in pa.get_device_info_generator():\n',
    '        pa = _acquire_pa()\n        try:\n            for info in pa.get_device_info_generator():\n'
)
s = s.replace(
    '                        is_loopback=False,\n                    )\n                )\n        return devices\n',
    '                        is_loopback=False,\n                    )\n                )\n        finally:\n            _release_pa()\n        return devices\n',
    1,
)
s = s.replace(
    '            with pyaudio.PyAudio() as pa:\n                return int(pa.get_default_wasapi_loopback()["index"])\n',
    '            pa = _acquire_pa()\n            try:\n                return int(pa.get_default_wasapi_loopback()["index"])\n            finally:\n                _release_pa()\n'
)
s = s.replace(
    '            with pyaudio.PyAudio() as pa:\n                return int(pa.get_default_input_device_info()["index"])\n',
    '            pa = _acquire_pa()\n            try:\n                return int(pa.get_default_input_device_info()["index"])\n            finally:\n                _release_pa()\n'
)

old = '''        try:\n            with pyaudio.PyAudio() as pa:\n                sample_width = pa.get_sample_size(sample_format)\n                with pa.open(\n                    format=sample_format,\n                    channels=self.device.channels,\n                    rate=self.device.sample_rate,\n                    input=True,\n                    input_device_index=self.device.index,\n                    frames_per_buffer=self.frames_per_buffer,\n                ) as stream:\n                    if self.health:\n                        self.health.log("INFO", f"capture:{self.source}", f"Аудиоустройство открыто: {self.device.name}")\n                    while not self.stop_event.is_set():\n'''
new = '''        pa = None\n        stream = None\n        try:\n            pa = _acquire_pa()\n            sample_width = pa.get_sample_size(sample_format)\n            with _PA_OPEN_LOCK:\n                stream = pa.open(\n                    format=sample_format,\n                    channels=self.device.channels,\n                    rate=self.device.sample_rate,\n                    input=True,\n                    input_device_index=self.device.index,\n                    frames_per_buffer=self.frames_per_buffer,\n                )\n            if self.health:\n                self.health.log("INFO", f"capture:{self.source}", f"Аудиоустройство открыто: {self.device.name}")\n            while not self.stop_event.is_set():\n'''
if old not in s:
    raise RuntimeError('Audio worker block not found')
s = s.replace(old, new)

# The old block was nested one level deeper under `with pa.open`; dedent its body.
start = s.index('            while not self.stop_event.is_set():\n', s.index('class AudioCaptureWorker'))
end = s.index('        except Exception as exc:\n            self._emit_error', start)
block = s[start:end]
lines = block.splitlines(True)
fixed = [lines[0]]
for line in lines[1:]:
    fixed.append(line[8:] if line.startswith('        ') else line)
s = s[:start] + ''.join(fixed) + s[end:]

s = s.replace(
    '        except Exception as exc:\n            self._emit_error(f"Не удалось открыть аудиоустройство «{self.device.name}»: {exc}")\n\n    def _flush_chunk',
    '''        except Exception as exc:\n            self._emit_error(f"Не удалось открыть аудиоустройство «{self.device.name}»: {exc}")\n        finally:\n            if stream is not None:\n                try:\n                    with _PA_OPEN_LOCK:\n                        stream.stop_stream()\n                        stream.close()\n                except Exception:\n                    pass\n            if pa is not None:\n                _release_pa()\n\n    def _flush_chunk'''
)

old_probe = '''    try:\n        with pyaudio.PyAudio() as pa:\n            with pa.open(\n                format=pyaudio.paInt16,\n                channels=device.channels,\n                rate=device.sample_rate,\n                input=True,\n                input_device_index=device.index,\n                frames_per_buffer=frames_per_buffer,\n            ) as stream:\n                total = 0\n                for _ in range(max(1, reads)):\n                    data = stream.read(frames_per_buffer, exception_on_overflow=False)\n                    total += len(data)\n        return True, f"устройство открывается и отдаёт аудиобуфер ({total} байт)"\n    except Exception as exc:\n        return False, f"не удалось открыть/прочитать устройство: {exc}"\n'''
new_probe = '''    pa = None\n    stream = None\n    try:\n        pa = _acquire_pa()\n        with _PA_OPEN_LOCK:\n            stream = pa.open(\n                format=pyaudio.paInt16,\n                channels=device.channels,\n                rate=device.sample_rate,\n                input=True,\n                input_device_index=device.index,\n                frames_per_buffer=frames_per_buffer,\n            )\n        total = 0\n        for _ in range(max(1, reads)):\n            data = stream.read(frames_per_buffer, exception_on_overflow=False)\n            total += len(data)\n        return True, f"устройство открывается и отдаёт аудиобуфер ({total} байт)"\n    except Exception as exc:\n        return False, f"не удалось открыть/прочитать устройство: {exc}"\n    finally:\n        if stream is not None:\n            try:\n                with _PA_OPEN_LOCK:\n                    stream.stop_stream()\n                    stream.close()\n            except Exception:\n                pass\n        if pa is not None:\n            _release_pa()\n'''
if old_probe not in s:
    raise RuntimeError('Audio probe block not found')
s = s.replace(old_probe, new_probe)
audio.write_text(s, encoding='utf-8')

ui = root / 'app' / 'ui.py'
t = ui.read_text(encoding='utf-8')
t = t.replace('self.enable_diarization.setChecked(default_model.exists())', 'self.enable_diarization.setChecked(False)')
old_start = '''    @Slot()\n    def start_session(self) -> None:\n        if self.running:\n            return\n'''
new_start = '''    @Slot()\n    def start_session(self) -> None:\n        if self.running:\n            return\n        try:\n            self._start_session_impl()\n        except Exception as exc:\n            try:\n                self.health.fatal("start", f"Ошибка запуска стенографии: {exc}")\n            except Exception:\n                pass\n            self.running = False\n            self.paused = False\n            self.timer.stop()\n            self.recording_label.setText("● ГОТОВ")\n            self.start_btn.setEnabled(True)\n            self.pause_btn.setEnabled(False)\n            self.stop_btn.setEnabled(False)\n            self._lock_settings(False)\n            QMessageBox.critical(\n                self,\n                "Не удалось начать стенографию",\n                "Программа не будет закрыта. Ошибка запуска:\\n\\n" + str(exc) +\n                "\\n\\nОткройте вкладку «Диагностика» для технических подробностей.",\n            )\n            self.refresh_health_view()\n\n    def _start_session_impl(self) -> None:\n'''
if old_start not in t:
    raise RuntimeError('UI start block not found')
t = t.replace(old_start, new_start)
ui.write_text(t, encoding='utf-8')

health = root / 'app' / 'health.py'
h = health.read_text(encoding='utf-8')
h = h.replace('APP_VERSION = "0.5"', 'APP_VERSION = "0.5.1"')
health.write_text(h, encoding='utf-8')

print('Hotfix 0.5.1 applied')
