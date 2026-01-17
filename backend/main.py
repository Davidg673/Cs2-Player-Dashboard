from fastapi import  FastAPI, Request
from fastapi.responses import  RedirectResponse
from urllib.parse import  urlencode
import  requests
from  fastapi.middleware.cors import  CORSMiddleware

app = FastAPI()

origins = [
    "http://localhost:5173"
]

app.middleware(  #Ensures local front end can call online backend
    CORSMiddleware,
    allow_origins = origins,
    allow_credentials = True,
    allow_methods = ["*"],
    allow_headers = ["*"],
)

STEAM_OPENID_URL = "https://steamcommunity.com/openid/login"
RETURN_URL = "http://localhost:8000/auth/steam/callback"
FRONTEND_URL = "http://localhost:5173/login-success"

@app.get("/")
def root():
    return {"status" : "FastAPI is running"}

@app.get("/auth/steam/login")   #Decorator used by fastapi to run the function below when given url is accessed
def steam_login():  #Sends request to steam for user to log in and retrieve the steam ID
    params = {
        "openid.ns" : "http://specs.openid.net/auth/2.0",
        "openid.mode" : "checkid_setup",
        "openid.return_to" : RETURN_URL,
        "openid.realm" : "http://localhost:8000",
        "openid.identity" : "http://specs.openid.net/auth/2.0/identifier_select",
        "openid.claimed_id" : "http://specs.openid.net/auth/2.0/identifier_select"
    }
    return RedirectResponse(f"{STEAM_OPENID_URL}?{urlencode(params)}") #returns url with data



@app.get("/auth//stea/callback")  #Return from steam authentication which receives data and attempts to validate info
def steam_return(request: Request):
    query = dict(request.query_params)

    query["openid.mode"] = "check_authentication"

    r = requests.post(STEAM_OPENID_URL, data= query)  #sends request back to steam for verification

    if "is_valid:true" not in r.text:
        return {"error" : "Invalid Steam login"}

    steam_id = query["openid.claimed_id"].split("/")[-1]

    return {"steam_id" : steam_id}  #back to front-end