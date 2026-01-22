import React from "react";

const LoginPage = ()=>{
    const handleLogin = () =>{
        window.location.href = "https://prewar-lavonne-gutsily.ngrok-free.dev/auth/steam/login";
    };

    return (
        <div className = "login-page"> 
            <h1> Welcome to CS2 Dashboard!</h1>
            <button onClick={handleLogin}>Login with Steam</button>
        </div>
    );

};

export default LoginPage;