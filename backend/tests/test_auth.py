import requests
from fastapi import FastAPI
from fastapi.testclient import TestClient

from app.routers.auth import router as auth_router
app = FastAPI()
app.include_router(auth_router)
client = TestClient(app)



def test_login_redirects_to_steam():
    response = client.get("/auth/steam/login",follow_redirects=False)

    assert response.status_code in (307,302)

    location = response.headers["location"]

    assert "https://steamcommunity.com/openid/login" in location

    assert "openid.ns=" in location
    assert "openid.mode=checkid_setup" in location
    assert "openid.return_to=" in location
    assert "openid.realm=" in location
    assert "openid.identity=" in location
    assert "openid.claimed_id=" in location
    assert "force_login=" in location



class FakeResponse:
    def __init__(self,text):
        self.text=text


def test_callback_invalid_login(monkeypatch):
    ##This replaces steam request method with a custom one which always returns false for testing negative case
    monkeypatch.setattr(requests,"post",lambda *args, **kwargs: FakeResponse("is_valid:false"))

    params = {
        "openid.claimed_id": "https://steamcommunity.com/openid/id/12345",
        "openid.mode":"id_res"
    }

    response = client.get("/auth/steam/callback",params=params,follow_redirects=False)

    location = response.headers["location"]

    assert response.status_code in (307,302)

    assert "/dashboard?error=invalid_login" in location


def test_callback_valid_login(monkeypatch):
    #Similar to above approach, however tests for positive case
    monkeypatch.setattr(requests,"post",lambda *args, **kwargs: FakeResponse("is_valid:true"))

    params = {
        "openid.claimed_id": "https://steamcommunity.com/openid/id/12345",
        "openid.mode": "id_res"
    }

    response = client.get("/auth/steam/callback", params=params, follow_redirects=False)

    location = response.headers["location"]

    assert response.status_code in (307, 302)

    assert "/dashboard?steam_id=" in location


