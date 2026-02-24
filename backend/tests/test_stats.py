from fastapi import FastAPI
from fastapi.testclient import TestClient
from fake_db import EngineRaisesOperational, FakeEngine,FakeConnStats
from app.routers.stats import router as stats_router
import app.routers.stats as stats_module

from datetime import datetime,timezone

app = FastAPI()
app.include_router(stats_router)
client = TestClient(app)

TEST_PAYLOAD = {
    "player":{
        "steamid":"12345",
        "last_played":datetime.now(timezone.utc).isoformat(),
        "kills":0,
        "deaths":0
    },
    "weapons":[
        {
            "weapon":"ak47",
            "kills":0
        },
        {
            "weapon":"awp",
            "kills":0
        }
    ]
}


def test_wrong_API_key_returns_401(monkeypatch):
    respone = client.post("/ingest/stats",
                         headers={"x-api-key":"WRONG_KEY"},
                         json = TEST_PAYLOAD
                     )

    assert respone.status_code == 401


def test_failed_db_operation_returns_500(monkeypatch):
    monkeypatch.setattr(stats_module,"INGEST_API_KEY","CORRECT_KEY")
    monkeypatch.setattr(stats_module,"engine",EngineRaisesOperational())

    respone = client.post("/ingest/stats",
                          headers={"x-api-key": "CORRECT_KEY"},
                          json=TEST_PAYLOAD
                      )

    assert respone.status_code == 500


def test_succesful_db_operation(monkeypatch):
    monkeypatch.setattr(stats_module,"INGEST_API_KEY","CORRECT_KEY")
    monkeypatch.setattr(stats_module,"engine",FakeEngine(FakeConnStats()))


    respone = client.post("/ingest/stats",
                          headers={"x-api-key": "CORRECT_KEY"},
                          json=TEST_PAYLOAD
                      )
    body = respone.json()

    assert respone.status_code == 200
    assert body["ok"] == True





