import React, { useState } from 'react';
import { PostNoneToken } from '../api/api';
import constant from '../constants/constants.js';
import { useNavigate } from 'react-router-dom';
import '../styles/LoginPage.css';

const LoginPage = () => {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [isError, setIsError] = useState(false);
  const [buttonLoad, setButtonLoad] = useState(false);

  const navigate = useNavigate();

  const isEmpty = (v) => !v || v.trim() === "";

  const login = async () => {
    setIsError(false);

    if (isEmpty(username) || isEmpty(password)) {
      setIsError(true);
      return;
    }

    setButtonLoad(true);

    let loginResponse;
        
    try {
      loginResponse = await PostNoneToken(
        "Auth/Login",
        { email: username, password }
      ).then(x => x.data);
    }
    catch {
      try {
        loginResponse = await PostNoneToken(
          "Auth/UpdateUser",
          { email: username, password }
        ).then(x => x.data);
      } catch {
        setIsError(true);
        setButtonLoad(false);
        return;
      }
    }

    if (!loginResponse?.token) {
      setIsError(true);
      setButtonLoad(false);
      return;
    }

    localStorage.setItem(constant.tokenKey2, loginResponse.token);
    localStorage.setItem("isLoggedIn", "true");

    setButtonLoad(false);
    navigate('/DataDisplay');
  };

  return (
    <div className='loginPageBody'>
      <div className="login-container login-page">
        {/* <img src={logo} className='login-image' /> */}

        <h2>GİRİŞ YAP</h2>

        <form onSubmit={(e) => e.preventDefault()}>
          <input
            placeholder='Kullanıcı Mail Adresi'
            type="text"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            style={{ textAlign: 'center' }}
          />

          <input
            placeholder='Şifre'
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            style={{ textAlign: 'center' }}
          />

          <br />

          <button onClick={login}>
            {buttonLoad ? "Giriş Yapılıyor..." : "Giriş Yap"}
          </button>

          {isError && <div className='text-danger'><b>Giriş hatalı!</b></div>}
        </form>
      </div>
    </div>
  );
};

export default LoginPage;
