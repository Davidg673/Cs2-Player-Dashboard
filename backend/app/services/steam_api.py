import os
import  requests
from dotenv import load_dotenv
from requests import RequestException

load_dotenv()

STEAM_API_KEY = os.getenv("STEAM_API_KEY","").strip()

if not STEAM_API_KEY:
    raise RuntimeError("Missing Steam API key env variable")

def get_player_name(steam_id: str) -> str | None:
    """
    Sends a request to steam for a steam name using given id

    :param steam_id: id of the player
    :return: steam name from steam db
    """

    url = "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/"
    params = {
        "key":STEAM_API_KEY,
        "steamids": steam_id
    }

    try:
        response = requests.get(url,params=params, timeout=5)
        response.raise_for_status()

        data = response.json() ##convert to python dictionary
        players = data.get("response", {}).get("players",[]) #use get here to automatically raise exceptions on entry not found.
                                                             # Non-existing values get replaced with second parameter after comma instead of raising KeyError

        if not players:
            return None

        return players[0]["personaname"]

    except RequestException as ex:
        print(f"Steam API request failed: {ex}") ##Used for backend debugging
        raise ex

    except (KeyError, TypeError, ValueError) as ex:
        print(f"Unexpected Steam response: {ex}") #Used for backend debugging
        raise  ex
