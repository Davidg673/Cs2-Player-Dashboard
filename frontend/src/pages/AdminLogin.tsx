import { useEffect, useState } from "react";
import Meteors from "../components/Meteors"
import { UserIcon ,LockIcon } from "lucide-react";
import { useNavigate } from "react-router-dom";


const AdminLoginPage = ()=>{
    const BACKEND = import.meta.env.VITE_BACKEND_BASE_URL;
    const [error,setError] = useState<string | null>(null);
    const [loading, setLoading] = useState(false);

    const navigate = useNavigate();




    const handleSubmit = (e: React.SyntheticEvent<HTMLFormElement>)=> {
        e.preventDefault();

        if (loading) return;
        
        setLoading(true);

        const formData = new FormData(e.currentTarget);

        const cleanUsername = (formData.get("username") as string).trim();
        const cleanPassword = (formData.get("password") as string).trim();

        validateData(cleanUsername,cleanPassword);
        
    };




    //Async send user details for validation
    const validateData = async (username:string, password:string)=> {
        const controller = new AbortController();

        setTimeout(() => controller.abort(), 5000);

        try{
            const res = await fetch(`${BACKEND}/login`,{
                method:"POST",
                headers: {
                    "Content-Type": "application/json"
                },
                credentials:"include", //include cookie
                body: JSON.stringify({username,password}), //converts to JSON string
                signal: controller.signal
            });

            if (res.ok){
                navigate("/admin");
            } else{
                const errorData = await res.json();
                setError(errorData.detail || "Login Failed");
            }
        
  
        }catch (err){
            setError("Network Error");
        } finally{
            setLoading(false);
        }
    }


    ///Cleans up banner after 3 seconds
    useEffect(() => {
        if (!error) return;

        const timer = setTimeout(() =>{
            setError(null);
        }, 3000);

        return () => clearTimeout(timer);
    },[error]);


    ////Directly sends user to admin page if they are logged in. Verifies cookie
    useEffect (() => {
        const checkAuth = async () => {
            try{
                const res = await fetch(`${BACKEND}/verify_cookie`,{
                    credentials: "include",
                    headers:{
                        "ngrok-skip-browser-warning": "True"
                    }
                });

                if (res.ok){
                    navigate("/admin");
                }
            } catch (err){

            }
        }
        
        checkAuth();
    },[]);


    return (    
        <div className="relative min-h-screen flex flex-col items-center justify-center">  

            {error && (
                <div className="relative z-10 w-full max-w-md p-4 rounded-lg bg-red-500/10 border border-red-500 text-red-400 mb-2 flex items-center justify-between">
                    <span>{error}</span>
                    <button className="ml-4 text-black hover:text-gray" onClick={() => setError(null)}>✕</button>
                </div>
            )}

            {/* Background */}
            <div className="absolute inset-0 z-0">
                <Meteors/>
            </div>
            
            {/* Login Card */}
            <form onSubmit={handleSubmit} className="relative z-10 w-full max-w-md p-8 bg-gray-900/80 rounded-2xl shadow-xl backdrop-blur-md border border-white/10 space-y-6">

                {/* Header */}
                <div className="text-center">
                    <h1 className="text-2xl font-semibold text-white">Admin Login</h1>
                    <p className="text-sm text-gray-400">Sign in to your admin dashboard</p>
                </div>

                {/* Username */}
                <div>
                    <label htmlFor="username" className="block mb-2 text-sm text-gray-300">Username</label>

                    <div className="relative">
                        <input
                            type = "text"
                            id="username"
                            name="username"
                            className="w-full pl-10 pr-3 py-3 rounded-lg bg-white/10 text-white placeholder-gray-400 border border-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition"
                            required
                        ></input>
                        <UserIcon className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-gray-400"></UserIcon>
                    </div>
                </div>
            
                {/* Password */}
                <div>
                    <label htmlFor="password" className="block mb-2 text-sm text-gray-300">Password</label>
                    
                    <div className="relative">
                        <input
                            type = "password"
                            id="password"
                            name="password"
                            className="w-full pl-10 pr-3 py-3 rounded-lg bg-white/10 text-white placeholder-gray-400 border border-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition"
                            required
                        ></input>
                        <LockIcon className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-gray-400"></LockIcon>        
                    </div>
                </div>           

                <button type="submit" disabled={loading} className="w-full p-3 bg-blue-600 hover:bg-blue-700 rounded-lg text-white font-medium transition">{loading ? "Logging in.." : "Login"}</button> 
            </form>
        </div>
    );

};

export default AdminLoginPage;