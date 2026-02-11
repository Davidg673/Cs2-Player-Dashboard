import React from "react";
import "./LoginPage.css"
import Meteors from "./components/Meteors"

const LoginPage = ()=>{
    const handleLogin = () =>{
        window.location.href = "https://prewar-lavonne-gutsily.ngrok-free.dev/auth/steam/login";
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