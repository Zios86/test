param(
    [Parameter(Mandatory=$true)][string]$ClientIP,
    [Parameter(Mandatory=$true)][string]$Token
)
$env:DION_POSTPROCESS_TOKEN = $Token
python .\postprocess_server.py --host 0.0.0.0 --port 8765 --allow-client $ClientIP --whisper-model large-v3-turbo --device cpu --compute-type int8 --ollama-url http://127.0.0.1:11434 --ollama-model qwen3:4b
