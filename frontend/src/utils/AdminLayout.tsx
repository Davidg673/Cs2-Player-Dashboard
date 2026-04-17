import { useEffect,useState } from "react";
import { Outlet, Navigate } from "react-router-dom";

const AdminLayout = () => {
    const [isAuth, setIsAuth] = useState<boolean | null> (null);
    const BACKEND = import.meta.env.VITE_BACKEND_BASE_URL;
    
    ////Sends HTTP request to backend to verify cookie
    const verify_cookie = async () =>{
        try{
            //Receives a response object with status(401,200), ok(true/false), body //Credentials includes cookies for verification
            const res = await fetch(`${BACKEND}/verify_cookie`,{ 
                method:"GET",
                credentials:"include",
                headers:{
                    "ngrok-skip-browser-warning": "True"
                }
            });

            setIsAuth(res.ok);
        } catch {
            setIsAuth(false);
        }
    };

    //Verifies cookie once mounted in browser
    useEffect(() => {
        verify_cookie();
    }, []);

    if (isAuth === null){
        return <div>Loading...</div>;
    }

    //Return user to root
    if (!isAuth){
        return <Navigate to="/" />;
    }

    //returns the child page (actual admin page)
    return <Outlet />;


};

export default AdminLayout; 