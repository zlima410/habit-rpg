import { Platform } from 'react-native';

export const STORAGE_KEYS = {
    AUTH_TOKEN: 'habitrpg_auth_token',
    USER_PREFERENCES: 'habitrpg_user_prefs',
} as const;

const isDevelopment = () => {
    return (
        __DEV__ ||
        process.env.NODE_ENV === 'development' ||
        process.env.EXPO_PUBLIC_ENV === 'development'
    );
};

const getApiUrl = () => {
  if (isDevelopment()) {
    if (Platform.OS === "ios") {
      const iosUrl = "http://localhost:5139" || process.env.EXPO_PUBLIC_API_URL_DEV_IP;
      console.log("📱 Using iOS API URL:", iosUrl);
      return iosUrl;
    }

    const androidUrl =
      "http://localhost:5139" ||
      (process.env.ENV_PUBLIC_LOCAL_IP ? `http://${process.env.ENV_PUBLIC_LOCAL_IP}:5139` : process.env.EXPO_PUBLIC_API_URL_DEV_IP);
    console.log("🤖 Using Android API URL:", androidUrl);
    return androidUrl;
  } else {
    const prodUrl = process.env.EXPO_PUBLIC_API_URL_PROD || "https://habitrpg.zlima.dev";
    console.log("🌐 Using Production API URL:", prodUrl);
    return prodUrl;
  }
};

export const API_CONFIG = {
  BASE_URL: getApiUrl(),
  TIMEOUT: 15000,
};

if (__DEV__) {
  console.log("🔧 API Configuration:", {
    BASE_URL: API_CONFIG.BASE_URL,
    TIMEOUT: API_CONFIG.TIMEOUT,
    PLATFORM: Platform.OS,
    IS_DEV: isDevelopment(),
  });
}