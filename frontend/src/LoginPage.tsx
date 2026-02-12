import React from "react";
import "./LoginPage.css"
import Meteors from "./components/Meteors"

const LoginPage = ()=>{
  const BACKEND = import.meta.env.VITE_BACKEND_BASE_URL;

    const handleLogin = () =>{
        window.location.href = `${BACKEND}/auth/steam/login`;
    };

    return (
        <>
            <Meteors/>
            <div className = "login-background"> 
                <h1> Welcome to CS2 Dashboard!</h1>
                <button onClick={handleLogin}>Login with Steam</button>
            </div>
        </>
    );

};

export default LoginPage;