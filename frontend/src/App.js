import React, { useState } from 'react';
import { BrowserRouter as Router, Route, Routes } from 'react-router-dom';
import DataDisplay from './components/DataDisplay';
import LoginPage from './components/LoginPage';

function App() {
  // State to manage user authentication status
  const [isLoggedIn, setLoggedIn] = useState(false);

  // Function to handle user login
  const handleLogin = () => {
    // Perform your login logic here
    setLoggedIn(true);
  };

  // Function to handle user logout
  const handleLogout = () => {
    // Perform your logout logic here
    setLoggedIn(false);
  };

  return (
    <div>
      <Router>
        <Routes>
          {/* Use a render function to pass authentication status to components */}
          <Route
            path="/"
            element={<LoginPage isLoggedIn={isLoggedIn} />}
          />
          <Route
            path="/DataDisplay"
            element={<DataDisplay isLoggedIn={isLoggedIn} />}
          />
        </Routes>
      </Router>
    </div>
  );
}

export default App;
