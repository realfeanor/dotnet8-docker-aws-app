const constants = {
  apiBaseUrl: `${import.meta.env.VITE_API_URL || "http://localhost:5000"}/api/`,
  authTokenKey: "stockroom.access_token",
};

export default constants;
