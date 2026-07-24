import { useState } from "react";
import Meteors from "./components/Meteors"
import { useNavigate } from "react-router-dom";

const AdminLoginPage = ()=>{
  const BACKEND = import.meta.env.VITE_BACKEND_BASE_URL;
  const navigate = useNavigate();




  async function handleSubmit(event: React.SubmitEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    const formData = new FormData(event.currentTarget);

    const username = formData.get("username");
    const pass = formData.get("password");

    if (username =="" || username !instanceof String || pass == "" || pass !instanceof String)
    {
        setSubmitErr("Empty fields");
        return;
    }

    if (username !instanceof String || pass !instanceof String)
    {
        console.log("Invalid Format");
        return;
    }

    try{
        const jsonData = {
            name: username,
            password: pass
        };

        const request = await fetch(`${BACKEND}/admin_login_verify`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(jsonData)
        });
        if (!request.ok)
            {
                throw new Error(`Response status: ${request.status}`);
            }
    
        const response: {status: string} = await request.json();

        if (response.status == "success")
        {
            navigate("/admin_dashboard");
        }
        else
        {
            setSubmitErr("Incorrect Details")
        }

        
    } catch(err)
    {
        if (err instanceof Error)
        {
            setSubmitErr("Internal Server Error");
        }
    }
  };



  const [submitErr, setSubmitErr] = useState<string>("");

    return (    
        <div className="relative min-h-screen flex items-center justify-center">  

            {/* Background */}
            <div className="absolute inset-0 z-0">
                <Meteors/>
            </div>
            <form onSubmit={handleSubmit} className="p-5 flex flex-col z-10 w-full max-w-md pt-5 pb-5   bg-gray-900 rounded-md backdrop-blur-sm border border-gray-100 space-y-4">
                <div className="mx-auto">
                    <h2 className="text-white">Admin Login</h2>
                </div>
                
                <div className="pl-5">
                    <label htmlFor="username" className="block mb-2 text-white">Username</label>
                    <input
                        type = "text"
                        id="username"
                        name="username"
                        className="w-4/5 p-2 rounded bg-white/10 text-white"
                        required
                    ></input>
                </div>

                <div className="pb-5 pl-5">
                    <label htmlFor="password" className="block mb-2 text-white">Password</label>
                    <input
                        type = "password"
                        id="password"
                        name="password"
                        className="w-4/5 p-2 rounded bg-white/10 text-white"
                        required
                    ></input>
                </div>           
                <div className="flex flex-col mx-auto">
                    <button type="submit" className="w-48 p-2 bg-blue-600 rounded text-white">Login</button> 
                    <h2 className="text-red-600">{submitErr}</h2>
                </div>
            </form>
        </div>
    );

};

export default AdminLoginPage;