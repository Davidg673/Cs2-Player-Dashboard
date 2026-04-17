import React from "react";
import { BrowserRouter as Router,Routes,Route, Navigate} from "react-router-dom";
import LoginPage from "./pages/LoginPage";
import StatsPage from "./pages/StatsPage";
import AdminLoginPage from "./pages/AdminLogin";
import AdminLayout from "./utils/AdminLayout";
import AdminBoard from "./pages/AdminBoard";

function App() {
  return (
    <Router>
      <Routes>
        <Route path="/" element={<Navigate to={"/home"}/>}/>
        <Route path="/home" element={<LoginPage/>}/>
        <Route path="/dashboard" element={<StatsPage/>}/>
        <Route path="/admin_login" element={<AdminLoginPage/>}/>
        <Route path="/admin" element={<AdminLayout />}> 
          <Route index element={<AdminBoard />} />
        </Route>
      </Routes>
    </Router>
  );
}

export default App;