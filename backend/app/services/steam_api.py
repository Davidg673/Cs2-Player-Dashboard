import  requests
from requests import RequestException


STEAM_API_KEY = "BA3233F31757A3833B588A637B26E9C9"


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
        players = data.get("response", {}).get("players",[]) #use get here to automatically raise exceptions on entry not found

        if not players:
            return None

        return players[0]["personaname"]

    except RequestException as ex:
        print(f"Steam API request failed: {ex}")
        return None

    except (KeyError, TypeError, ValueError) as ex:
        print(f"Unexpected Steam response: {ex}")
        return  None
