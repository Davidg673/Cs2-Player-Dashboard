from fastapi import FastAPI
from fastapi.testclient import TestClient

from app.routers.players import router as players_router
import app.routers.players as players_module

from tests.fake_db import FakeEngine, FakePlayerResult, FakeWeaponsResult, FakePlayerResultValid, \
    FakeWeaponsResultValid, FakeConnPlayers, EngineRaisesOperational, EngineRaisesSQLAlchemy

app = FastAPI()
app.include_router(players_router)
client = TestClient(app)



def test_player_not_found_returns_404(monkeypatch):
    monkeypatch.setattr(players_module,"get_player_name",lambda steam_id:None)
    engine = FakeEngine(FakeConnPlayers([
        FakePlayerResult(),
        FakeWeaponsResult()
    ]))
    monkeypatch.setattr(players_module,"engine",engine)


    response = client.get("/player/-99999")

    assert response.status_code == 404



def test_db_unavailable_returns_503(monkeypatch):
    monkeypatch.setattr(players_module, "get_player_name", lambda steam_id: None)
    monkeypatch.setattr(players_module, "engine", EngineRaisesOperational())

    response = client.get("/player/12345")

    assert response.status_code == 503
    assert response.json()["detail"] == "Database service is unavailable"



def test_server_err_returns_503(monkeypatch):
    monkeypatch.setattr(players_module, "get_player_name", lambda steam_id: None)
    monkeypatch.setattr(players_module, "engine", EngineRaisesSQLAlchemy())

    response = client.get("/player/12345")

    assert response.status_code == 503
    assert response.json()["detail"] == "An unexpected server error has occurred"



def test_valid_data_received(monkeypatch):
    monkeypatch.setattr(players_module,"get_player_name",lambda steam_id:"player1")
    engine = FakeEngine(FakeConnPlayers([
        FakePlayerResultValid(),
        FakeWeaponsResultValid()
    ]))
    monkeypatch.setattr(players_module,"engine",engine)


    response = client.get("/player/12345")
    body = response.json()

    assert response.status_code == 200

    assert "player" in body
    assert "weapons" in body

    weapons = body["weapons"]
    player = body["player"]

    #Return Type
    assert isinstance(player,dict)
    assert isinstance(weapons,list)
    assert isinstance(weapons[0],dict)

    #Assert return keys are correct
    for key in ("steamid","kills","deaths","assists","headshots","damage_dealt","damage_received","bomb_plants",
                "bomb_defuses","playtime","last_played","rounds_won","rounds_lost"):
        assert key in player

    #Assert Player Values
    assert player["steamid"] == "12345"
    assert player["kills"] == 10
    assert player["deaths"] == 5
    assert player["assists"] == 2
    assert player["rounds_won"] == 8

    #Check datetime
    assert isinstance(player["last_played"], (str,type(None)))

    #Check weapon keys
    for key in ("steamid", "weapon", "kills", "headshots", "shots_hit", "damage_dealt"):
        assert key in weapons[0]

    #Check weapon values
    assert weapons[0]["weapon"] == "ak47"
    assert weapons[0]["kills"] == 7