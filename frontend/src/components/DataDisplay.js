import React from "react";
import "../styles/DataDisplay.css";
import { useNavigate } from "react-router-dom";

const DataDisplay = () => {
  const navigate = useNavigate();
  const currentDate = new Date().toLocaleDateString("tr-TR");

  const logout = () => {
    localStorage.removeItem("isLoggedIn");
    localStorage.removeItem("token");
    navigate("/");
  };

  return (
    <div className="welcome-page">
      <div className="welcome-card">
        <h1>Hoş Geldiniz</h1>

        <p className="welcome-text">
          Sisteme başarıyla giriş yaptınız.
        </p>

        <p className="date-text">
          Tarih: {currentDate}
        </p>

        <div className="info-box">
          <p>Bu ekran şu anda örnek karşılama sayfasıdır.</p>
          <p>Yeni modüller daha sonra eklenecektir.</p>
        </div>

        <button className="logout-btn" onClick={logout}>
          Çıkış Yap
        </button>
      </div>
    </div>
  );
};

export default DataDisplay;