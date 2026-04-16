import React from "react";
import { BrowserRouter as Router,Routes,Route, Navigate} from "react-router-dom";
import LoginPage from "./LoginPage";
import StatsPage from "./StatsPage";
import AdminLoginPage from "./AdminLogin";

function App() {
  return (
    <Router>
      <Routes>
        <Route path="/" element={<Navigate to={"/login"}/>}/>
        <Route path="/login" element={<LoginPage/>}/>
        <Route path="/dashboard" element={<StatsPage/>}/>
        <Route path="/admin_login" element={<AdminLoginPage/>}/>
      </Routes>
    </Router>
  );
}

export default App;