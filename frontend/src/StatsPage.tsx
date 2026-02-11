import React from "react";
import "./StatsPage.css"
import { useEffect,useState } from "react";
import {useLocation } from "react-router-dom";


const StatsPage = () =>{
  const [playerData,setPlayerData] = useState<any>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const location = useLocation();

  const reLogin = () =>{
      window.location.href = "https://prewar-lavonne-gutsily.ngrok-free.dev/auth/steam/login";
  };

  useEffect(()=> {
    const params = new URLSearchParams(location.search);
    const steamID = params.get("steam_id");

    if (!steamID)
    {
      setError("Steam ID not found");
      setLoading(false);
      return;
    }

    const fetchPlayer = async () =>{
    try
      {
        console.log("Fetching player data for: ", steamID);
        
        const res = await fetch(`http://localhost:8000/player/${steamID}`);
        console.log("Response status: ", res.status);

        const data = await res.json();
        console.log("Fetched data: ", data);

        if (!res.ok){
          setError(data.detail || "Unknown server error");
          return;
        }

        setPlayerData(data);
      }
      catch (err)
      {
        console.error("Fetch error: ", err)
        setError((err as Error).message);
      } finally {
        setLoading(false);
      }
    };

    fetchPlayer();

  },[location.search]);


  if (error) return(
    <div className="error-center">
      <p>{error}</p>
      <button onClick={reLogin}>Change Account</button>
    </div>
  );

  const PlayerStatsCard = ({label , value} :  {label : string, value : string | null}) =>{
    return (
      <div className="center-highlight">
          <h2>{label}: {value}</h2>
      </div>
    );
  };

  
  const WeaponStatsCard = ({name,kills,headshots,shotsHit,damageDealt} :  {name: string, kills: string | undefined,headshots: string | undefined, 
                                                                          shotsHit: string | undefined, damageDealt: string | undefined}) =>{
    return (
      <div>
          <div className="center-box-sub">
            <image href=""/>
          </div>
          <div className="center-box-sub">
            <h2>Weapon: {name}</h2> 
            <h2>Kills: {kills}</h2>
            <h2>headshots: {headshots}</h2>
            <h2>Shots Hit: {shotsHit}</h2>
            <h2>Damage Dealt: {damageDealt}</h2>
          </div>
      </div>
    );
  };


  const normalizeDate = (dateStr:string | null): string => {
    if (!dateStr) return "";
    const newDate = new Date(dateStr);
    return newDate?.toLocaleString();
  }
  
  const normalizePlaytime = (time : number | null): string =>{
      if (!time) return "";
      const hours = Math.floor(time/3600);
      const minutes = Math.floor((time%3600)/60);
      const seconds  = time % 60;

      return `${hours}h:${minutes}m:${seconds}s`;
  }


  if (loading) return <div className="error-center"><p>Loading...</p></div>

  if (!error) return (
    <>
      <div className='app-background'>
        <div className='top-banner'>
          <div className="top-banner-sub">
            <img className='player-image' src='/operator.png' alt="Operator"></img>
            <h1> {playerData?.player.steamid}</h1>
          </div> 
          <div className="top-banner-sub" >
            <h2>Total Playtime: {normalizePlaytime(playerData?.player.playtime)}</h2>
            <h2 style={{paddingLeft: "1rem"}}>Last Played: {normalizeDate(playerData?.player.last_played)}</h2>
          </div>
        </div>
        <div className="center-panel">
          <div>
          <PlayerStatsCard label="Kills" value={playerData?.player.kills}></PlayerStatsCard>
          <PlayerStatsCard label="Deaths" value={playerData?.player.deaths}></PlayerStatsCard>
          <PlayerStatsCard label="Assists" value={playerData?.player.assists}></PlayerStatsCard>
          <PlayerStatsCard label="Headshots" value={playerData?.player.headshots}></PlayerStatsCard>
          <PlayerStatsCard label="Damage Dealt" value={playerData?.player.damage_dealt}></PlayerStatsCard>
          <PlayerStatsCard label="Damage Received" value={playerData?.player.damage_received}></PlayerStatsCard>
          <PlayerStatsCard label="Bomb Plants" value={playerData?.player.bomb_plants}></PlayerStatsCard>
          <PlayerStatsCard label="Bomb Defuses" value={playerData?.player.bomb_defuses}></PlayerStatsCard>
          <PlayerStatsCard label="Rounds Won" value={playerData?.player.rounds_won}></PlayerStatsCard>
          <PlayerStatsCard label="Rounds Lost" value={playerData?.player.rounds_lost}></PlayerStatsCard>
          </div>
          <div className="center-box">
          {playerData?.weapons?.map((weapon:any) =>(
            <WeaponStatsCard
              key={weapon.weapon}
              name={weapon.weapon}
              kills={weapon.kills}
              headshots={weapon.headshots}
              shotsHit={weapon.shots_hit}
              damageDealt={weapon.damage_dealt}
            />
          ))}
          </div>
        </div>
      </div>
    </>
  )
};

export default StatsPage;


