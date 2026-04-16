import Meteors from "./components/Meteors"

const AdminLoginPage = ()=>{
  const BACKEND = import.meta.env.VITE_BACKEND_BASE_URL;

  const handleSubmit = ()=> {

  };

    return (    
        <div className="relative min-h-screen flex items-center justify-center">  

            {/* Background */}
            <div className="absolute inset-0 z-0">
                <Meteors/>
            </div>

            <form onSubmit={handleSubmit} className="relative z-10 w-full max-w-md bg-gray-900 rounded-md backdrop-blur-sm border border-gray-100 space-y-4">
                <div>
                    <label htmlFor="username" className="block mb-2 text-white">Username</label>
                    <input
                        type = "text"
                        id="username"
                        name="username"
                        className="w-1/2 p-2 rounded bg-white/10 text-white"
                        required
                    ></input>
                </div>

                <div>
                    <label htmlFor="password" className="block mb-2 text-white">Password</label>
                    <input
                        type = "password"
                        id="password"
                        name="password"
                        className="w-1/2 p-2 rounded bg-white/10 text-white"
                        required
                    ></input>
                </div>           

                <button type="submit" className="w-full p-2 bg-blue-600 rounded text-white">Login</button> 
            </form>
        </div>
    );

};

export default AdminLoginPage;