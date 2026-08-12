import os
import requests
import re
import time
from bs4 import BeautifulSoup
from urllib.parse import quote
from config import HEADERS, BASE_PATH

def fetch_steam_app_id(game_name):
    """Get Steam app ID for a game."""
    try:
        search_url = f"https://steamcommunity.com/actions/SearchApps/{quote(game_name)}"
        response = requests.get(search_url, headers=HEADERS, timeout=10)
        response.raise_for_status()

        apps = response.json()
        if apps:
            return apps[0]['appid']
        return None
    except Exception:
        return None

def fetch_steam_cover_image(app_id, game_name, game_folder):
    """Download cover image from Steam."""
    try:
        cover_url = f"https://cdn.akamai.steamstatic.com/steam/apps/{app_id}/library_600x900_2x.jpg"
        response = requests.get(cover_url, headers=HEADERS, timeout=10)

        if response.status_code == 200:
            cover_path = os.path.join(game_folder, "cover.jpg")
            with open(cover_path, 'wb') as f:
                f.write(response.content)
            return True

        # Try alternative URL
        cover_url_alt = f"https://cdn.akamai.steamstatic.com/steam/apps/{app_id}/header.jpg"
        response = requests.get(cover_url_alt, headers=HEADERS, timeout=10)

        if response.status_code == 200:
            cover_path = os.path.join(game_folder, "cover.jpg")
            with open(cover_path, 'wb') as f:
                f.write(response.content)
            return True

        return False
    except Exception:
        return False

def fetch_gog_cover_image(game_name, game_folder):
    """Download cover image from GOG as a fallback."""
    try:
        formatted_name = re.sub(r'[^a-zA-Z0-9]+', '_', game_name.lower()).strip('_')
        url = f"https://www.gog.com/en/game/{formatted_name}"
        response = requests.get(url, headers=HEADERS, timeout=10)

        if response.status_code == 200:
            soup = BeautifulSoup(response.content, 'html.parser')
            meta_img = soup.find('meta', {'property': 'og:image'})
            if meta_img and meta_img.get('content'):
                img_url = meta_img['content']
                img_response = requests.get(img_url, headers=HEADERS, timeout=10)
                if img_response.status_code == 200:
                    cover_path = os.path.join(game_folder, "cover.jpg")
                    with open(cover_path, 'wb') as f:
                        f.write(img_response.content)
                    return True
        return False
    except requests.RequestException as e:
        print(f"  GOG Request Error: {e}")
        return False
    except Exception as e:
        print(f"  GOG Parsing Error: {e}")
        return False


def fetch_gsr_cover_image(game_name, game_folder):
    """Download cover image from GameSystemRequirements as a fallback."""
    try:
        formatted_name = re.sub(r'[^a-zA-Z0-9]+', '-', game_name.lower()).strip('-')
        url = f"https://gamesystemrequirements.com/game/{formatted_name}"
        response = requests.get(url, headers=HEADERS, timeout=10)

        if response.status_code == 200:
            soup = BeautifulSoup(response.content, 'html.parser')
            meta_img = soup.find('meta', {'property': 'og:image'})
            if meta_img and meta_img.get('content'):
                img_url = meta_img['content']
                img_response = requests.get(img_url, headers=HEADERS, timeout=10)
                if img_response.status_code == 200:
                    cover_path = os.path.join(game_folder, "cover.jpg")
                    with open(cover_path, 'wb') as f:
                        f.write(img_response.content)
                    return True
        return False
    except requests.RequestException as e:
        print(f"  GSR Request Error: {e}")
        return False
    except Exception as e:
        print(f"  GSR Parsing Error: {e}")
        return False


def fetch_wikipedia_cover_image(game_name, game_folder):
    """Download cover image from Wikipedia as a fallback."""
    try:
        search_url = "https://en.wikipedia.org/wiki/Special:Search"
        response = requests.get(search_url, params={'search': game_name}, headers=HEADERS, timeout=10)

        if response.status_code == 200:
            soup = BeautifulSoup(response.content, 'html.parser')
            box = soup.find('table', {'class': 'infobox'})
            if box:
                img = box.find('img')
                if img and 'src' in img.attrs:
                    img_url = img['src']
                    if img_url.startswith('//'):
                        img_url = 'https:' + img_url
                    elif img_url.startswith('/'):
                        img_url = 'https://en.wikipedia.org' + img_url

                    # Convert thumbnail URL to slightly larger version if possible
                    img_url = img_url.replace('/220px-', '/500px-').replace('/250px-', '/500px-')

                    img_response = requests.get(img_url, headers=HEADERS, timeout=10)
                    if img_response.status_code == 200:
                        cover_path = os.path.join(game_folder, "cover.jpg")
                        with open(cover_path, 'wb') as f:
                            f.write(img_response.content)
                        return True
        return False
    except requests.RequestException as e:
        print(f"  Wikipedia Request Error: {e}")
        return False
    except Exception as e:
        print(f"  Wikipedia Parsing Error: {e}")
        return False


def fetch_pcgw_cover_image(game_name, game_folder):
    """Download cover image from PCGamingWiki API."""
    try:
        url = "https://www.pcgamingwiki.com/w/api.php"
        # Using standard MediaWiki API to get the main page image
        params = {
            "action": "query",
            "prop": "pageimages",
            "titles": game_name,
            "format": "json",
            "pithumbsize": "800" # Target high-res
        }
        response = requests.get(url, params=params, headers=HEADERS, timeout=10)

        if response.status_code == 200:
            data = response.json()
            pages = data.get("query", {}).get("pages", {})
            for page_id, page_data in pages.items():
                # If page is found and has a thumbnail
                if page_id != "-1" and "thumbnail" in page_data:
                    img_url = page_data["thumbnail"]["source"]

                    img_response = requests.get(img_url, headers=HEADERS, timeout=10)
                    if img_response.status_code == 200:
                        cover_path = os.path.join(game_folder, "cover.jpg")
                        with open(cover_path, 'wb') as f:
                            f.write(img_response.content)
                        return True
        return False
    except requests.RequestException as e:
        print(f"  PCGW Request Error: {e}")
        return False
    except Exception as e:
        print(f"  PCGW Error: {e}")
        return False


def fetch_opencritic_cover_image(game_name, game_folder):
    """Download cover image from OpenCritic public API."""
    try:
        search_url = "https://api.opencritic.com/api/game/search"
        response = requests.get(search_url, params={'criteria': game_name}, headers=HEADERS, timeout=10)

        if response.status_code == 200:
            results = response.json()
            if results and len(results) > 0:
                game_id = results[0].get('id')

                if game_id:
                    game_url = f"https://api.opencritic.com/api/game/{game_id}"
                    game_response = requests.get(game_url, headers=HEADERS, timeout=10)

                    if game_response.status_code == 200:
                        game_data = game_response.json()
                        img_url = None

                        # OpenCritic stores boxArt (vertical) and banner images
                        images = game_data.get('images', {})
                        if 'boxArt' in images and images['boxArt'].get('og'):
                            img_url = f"https://img.opencritic.com/{images['boxArt']['og']}"
                        elif 'bannerImageUrl' in game_data:
                            img_url = game_data['bannerImageUrl']
                            if img_url and not img_url.startswith('http'):
                                img_url = f"https://img.opencritic.com/{img_url}"

                        if img_url:
                            img_req = requests.get(img_url, headers=HEADERS, timeout=10)
                            if img_req.status_code == 200:
                                cover_path = os.path.join(game_folder, "cover.jpg")
                                with open(cover_path, 'wb') as f:
                                    f.write(img_req.content)
                                return True
        return False
    except requests.RequestException as e:
        print(f"  OpenCritic Request Error: {e}")
        return False
    except Exception as e:
        print(f"  OpenCritic Error: {e}")
        return False


def fetch_lutris_cover_image(game_name, game_folder):
    """Download cover image from Lutris (Linux gaming DB) via HTML scraping."""
    try:
        search_url = f"https://lutris.net/games/?q={quote(game_name)}"
        response = requests.get(search_url, headers=HEADERS, timeout=10)

        if response.status_code == 200:
            soup = BeautifulSoup(response.content, 'html.parser')
            # Look for game links in their search results
            game_links = soup.find_all('a', href=re.compile(r'^/games/[^/]+/$'))

            for link in game_links:
                img = link.find('img')
                if img and 'src' in img.attrs and img['src']:
                    img_url = img['src']
                    if img_url.startswith('/'):
                        img_url = "https://lutris.net" + img_url

                    img_response = requests.get(img_url, headers=HEADERS, timeout=10)
                    if img_response.status_code == 200:
                        cover_path = os.path.join(game_folder, "cover.jpg")
                        with open(cover_path, 'wb') as f:
                            f.write(img_response.content)
                        return True
        return False
    except requests.RequestException as e:
        print(f"  Lutris Request Error: {e}")
        return False
    except Exception as e:
        print(f"  Lutris Error: {e}")
        return False

def download_covers_step(base_path=BASE_PATH):
    """Step 3: Download actual cover images."""
    print("\n" + "=" * 70)
    print("STEP 3: DOWNLOADING ACTUAL COVER IMAGES")
    print("=" * 70 + "\n")

    if not os.path.exists(base_path):
        print("✗ Base path does not exist")
        return

    game_folders = sorted([f for f in os.listdir(base_path)
                          if os.path.isdir(os.path.join(base_path, f))])

    print(f"Found {len(game_folders)} game folders\n")

    downloaded = 0
    already_exists = 0
    failed = 0

    for idx, game_folder in enumerate(game_folders, 1):
        game_name = game_folder
        game_path = os.path.join(base_path, game_folder)
        cover_path = os.path.join(game_path, "cover.jpg")

        if os.path.exists(cover_path):
            already_exists += 1
            continue

        try:
            print(f"[{idx}/{len(game_folders)}] {game_name}...", end=" ", flush=True)

            app_id = fetch_steam_app_id(game_name)
            if app_id:
                if fetch_steam_cover_image(app_id, game_name, game_path):
                    print("✓ Steam")
                    downloaded += 1
                    time.sleep(0.2)
                    continue

            # Fallback to GOG
            if fetch_gog_cover_image(game_name, game_path):
                print("✓ GOG")
                downloaded += 1
                time.sleep(0.2)
                continue

            # Fallback to GameSystemRequirements (GSR)
            if fetch_gsr_cover_image(game_name, game_path):
                print("✓ GameSystemRequirements")
                downloaded += 1
                time.sleep(0.2)
                continue

            # Fallback to Wikipedia
            if fetch_wikipedia_cover_image(game_name, game_path):
                print("✓ Wikipedia")
                downloaded += 1
                time.sleep(0.2)
                continue

            # Fallback to PCGamingWiki
            if fetch_pcgw_cover_image(game_name, game_path):
                print("✓ PCGamingWiki")
                downloaded += 1
                time.sleep(0.2)
                continue

            # Fallback to OpenCritic
            if fetch_opencritic_cover_image(game_name, game_path):
                print("✓ OpenCritic")
                downloaded += 1
                time.sleep(0.2)
                continue

            # Fallback to Lutris
            if fetch_lutris_cover_image(game_name, game_path):
                print("✓ Lutris")
                downloaded += 1
                time.sleep(0.2)
                continue

            print("✗ Failed")
            failed += 1

        except Exception:
            print("✗ Error")
            failed += 1

    print(f"\n✓ Already exists: {already_exists} images")
    print(f"✓ Downloaded: {downloaded} images")
    print(f"✗ Failed: {failed} images")
