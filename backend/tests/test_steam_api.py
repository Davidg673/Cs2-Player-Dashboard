import pytest
from  fastapi import  FastAPI
from fastapi.testclient import TestClient
from requests.exceptions import HTTPError, RequestException
from app.routers.auth import router as auth_router
import app.services.steam_api as steam_api


app = FastAPI()
app.include_router(auth_router)
client = TestClient(app)

STEAM_API_KEY = ""

class FakeResponse:
    def __init__(self,json_data,status_code=200):
        self._json = json_data
        self.status_code = status_code

    def json(self):
        if self.status_code <300:
            return self._json
        else:
            raise ValueError()

    def raise_for_status(self):
        if self.status_code >= 400:
            raise HTTPError()


def test_steamapi_success_return_player_name(monkeypatch):
    json_data = {
        "response":{
            "players":[
                {
                    "personaname":"player1"
                }
            ]
        }
    }
    monkeypatch.setattr(steam_api.requests,"get",lambda *args, **kwargs :FakeResponse(json_data,200))

    response = steam_api.get_player_name("12345")

    assert response == "player1"

def test_player_not_found(monkeypatch):
    json_data = {
        "response": {
            "players": []
        }
    }
    monkeypatch.setattr(steam_api.requests,"get",lambda *args, **kwargs :FakeResponse(json_data,200))

    response = steam_api.get_player_name("12345")

    assert response is None


def test_steamapi_request_failed(monkeypatch):
    monkeypatch.setattr(steam_api.requests, "get", lambda *args, **kwargs: FakeResponse({},400))

    with pytest.raises(RequestException):
        steam_api.get_player_name("12345")


def test_unexpected_key_error(monkeypatch):
    json_data = {
        "response":{
            "players":[
                {}
            ]
        }
    }
    monkeypatch.setattr(steam_api.requests, "get", lambda *args, **kwargs: FakeResponse(json_data,200))

    with pytest.raises(KeyError):
        steam_api.get_player_name("12345")


def test_unexpected_type_error(monkeypatch):
    json_data = {
        "response":{
            "players": "not a list"
        }
    }
    monkeypatch.setattr(steam_api.requests, "get", lambda *args, **kwargs: FakeResponse(json_data,200))

    with pytest.raises(TypeError):
        steam_api.get_player_name("12345")

def test_unexpected_value_error(monkeypatch):
    monkeypatch.setattr(steam_api.requests, "get", lambda *args, **kwargs: FakeResponse({},300))

    with pytest.raises(ValueError):
        steam_api.get_player_name("12345")

