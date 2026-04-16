import "./LoginPage.css"
import Meteors from "./components/Meteors"

const LoginPage = ()=>{
  const BACKEND = import.meta.env.VITE_BACKEND_BASE_URL;

    const handleLogin = () =>{
        window.location.href = `${BACKEND}/auth/steam/login`;
    };

    const handleAdminlogin = () =>{
        window.location.href= `/admin_login`
    };

    return (
        <>
            <Meteors/>
            <div className = "login-background"> 
                <h1> Welcome to CS2 Dashboard!</h1>
                <button onClick={handleLogin}>Login with Steam</button>
                <button className="mt-5" onClick={handleAdminlogin}>Admin Login</button>
            </div>
        </>
    );

};

export default LoginPage;