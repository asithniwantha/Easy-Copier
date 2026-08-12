BASE_PATH = "./pc_games"

HEADERS = {
    'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36'
}

# Predefined system requirements for popular games
GAME_REQUIREMENTS = {
    "The Witcher 3 Wild Hunt": {
        "minimum": {"CPU": "CPU 64-bit: Intel i7 or AMD equivalent", "GPU": "NVIDIA GTX 960 or AMD Radeon R9 290", "RAM": "8 GB", "Storage": "136 GB"},
        "recommended": {"CPU": "CPU 64-bit: Intel i7 or AMD equivalent", "GPU": "NVIDIA GTX 1060 or AMD Radeon RX 480", "RAM": "16 GB", "Storage": "136 GB"}
    },
}
