import os
from dotenv import load_dotenv
from fastapi import Request, APIRouter
from fastapi.responses import  RedirectResponse
from urllib.parse import  urlencode
import  requests

load_dotenv()

BACKEND_URL = os.getenv("BACKEND_URL")
FRONTEND_URL = os.getenv("FRONTEND_URL")

STEAM_OPENID_URL = "https://steamcommunity.com/openid/login"

missing = [k for k,v in {
    "BACKEND_URL":BACKEND_URL,
    "FRONTEND_URL":FRONTEND_URL,
}.items() if not v]

if missing:
    raise RuntimeError(f"Missing env variables: {', '.join(missing)}")

router = APIRouter(tags=["auth"])

@router.get("/auth/steam/login")   #Decorator used by fastapi to run the function below when given url is accessed
def steam_login():  #Sends request to steam for user to log in and retrieve the steam ID
    params = {
        "openid.ns" : "http://specs.openid.net/auth/2.0",
        "openid.mode" : "checkid_setup",
        "openid.return_to" : f"{BACKEND_URL}/auth/steam/callback",
        "openid.realm" : f"{BACKEND_URL}",
        "openid.identity" : "http://specs.openid.net/auth/2.0/identifier_select",
        "openid.claimed_id" : "http://specs.openid.net/auth/2.0/identifier_select",
        "force_login" : "true"
    }
    return RedirectResponse(f"{STEAM_OPENID_URL}?{urlencode(params)}") #returns url with data



@router.get("/auth/steam/callback")  #Return from steam authentication which receives data and attempts to validate info
def steam_return(request: Request):
    query = dict(request.query_params)

    query["openid.mode"] = "check_authentication"

    r = requests.post(STEAM_OPENID_URL, data= query, timeout=5)  #sends request back to steam for verification

    if "is_valid:true" not in r.text:
        return RedirectResponse(f"{FRONTEND_URL}/dashboard?error=invalid_login")

    steam_id = query["openid.claimed_id"].split("/")[-1]

    return RedirectResponse(f"{FRONTEND_URL}/dashboard?steam_id={steam_id}")  #back to front-end
