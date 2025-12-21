import axios, { AxiosResponse, AxiosError } from 'axios';
import { API_CONFIG } from '../config';
import { TokenManager } from './tokenManager';
import { OfflineStorage } from '../utils/offlineStorage';
import { OfflineQueue } from '../utils/offlineQueue';
import NetInfo from '@react-native-community/netinfo';
import {
    AuthResponse,
    LoginRequest,
    RegisterRequest,
    ApiError
} from '../types/types';

const api = axios.create({
    baseURL: API_CONFIG.BASE_URL + "/api",
    timeout: API_CONFIG.TIMEOUT,
    headers: {
        'Content-Type': 'application/json',
    },
});

const isOffline = async (): Promise<boolean> => {
    const state = await NetInfo.fetch();
    return !state.isConnected || state.isInternetReachable === false;
}

const getCacheKey = (config: AxiosRequestConfig): string => {
    const { method, url, params } = config;
    const paramsStr = params ? JSON.stringify(params) : '';
    return `${method}_${url}_${paramsStr}`;
}

api.interceptors.request.use(async (config) => {
    try {
        const token = await TokenManager.getToken();

        if (token) {
            if (TokenManager.isTokenExpired(token)) {
                console.warn("⚠️ Token expired, removing...");
                await TokenManager.removeToken();
            } else {
                config.headers.Authorization = `Bearer ${token}`;
            }
        }

        const offline = await isOffline();
        if (offline && config.method !== "get") {
          await OfflineQueue.enqueue({
            method: config.method?.toUpperCase() || "GET",
            url: config.url || "",
            data: config.data,
            config: {
              headers: config.headers,
              params: config.params,
            },
          });

          return Promise.reject({
            message: "No internet connection. Request queued for later.",
            isOffline: true,
          });
        }
    } catch (error) {
        console.error("❌ Error adding auth token to request:", error);
    }

    return config;
});

api.interceptors.response.use(
    async (response: AxiosResponse) => {
        if (__DEV__)
            console.log(`✅ API Success: ${response.config.method?.toUpperCase()} ${response.config.url}`);

        if (response.config.method?.toLowerCase() === "get") {
          const cacheKey = getCacheKey(response.config);
          await OfflineStorage.set(cacheKey, response.data);
        }

        return response;
    },
    async (error: AxiosError) => {
        if (__DEV__) {
            console.log(`❌ API Error: ${error.config?.method?.toUpperCase()} ${error.config?.url}`);
            console.log("Error details:", error.response?.data);
        }

        if (!error.response && error.request) {
          const offline = await isOffline();

          if (offline && error.config) {
            if (error.config.method?.toLowerCase() === "get") {
              const cacheKey = getCacheKey(error.config);
              const cachedData = await OfflineStorage.get(cacheKey);

              if (cachedData) {
                console.log("📦 Returning cached data for:", error.config.url);
                return Promise.resolve({
                  ...error.response,
                  data: cachedData,
                  status: 200,
                  statusText: "OK (Cached)",
                  headers: {},
                  config: error.config,
                } as AxiosResponse);
              }
            }

            if (error.config.method && error.config.method.toLowerCase() !== "get") {
              await OfflineQueue.enqueue({
                method: error.config.method.toUpperCase(),
                url: error.config.url || "",
                data: error.config.data,
                config: {
                  headers: error.config.headers,
                  params: error.config.params,
                },
              });
            }
          }
        }

        if (error.response?.status == 401) {
            console.warn("🔐 Authentication failed, clearing token...");
            await TokenManager.removeToken();
        }

        return Promise.reject(error);
    }
);

const handleApiError = (error: AxiosError): ApiError => {
    if (error.response) {
        const data = error.response.data as any;

        if (data?.detail) {
            return {
                message: data.detail,
                status: error.response.status
            };
        }

        if (error.response.status === 400 && data?.errors) {
            const validationErrors = Object.values(data.errors).flat();
            return {
                message: validationErrors[0] as string || 'Validation error',
                status: error.response.status
            };
        }

        if (data?.title) {
            return {
                message: data.title,
                status: error.response.status
            }
        }

        if (data?.message) {
            return {
                message: data.message,
                status: error.response.status
            }
        }

        return {
            message: `Request failed with status ${error.response.status}`,
            status: error.response.status
        }
    } else if (error.request) {
        console.error("❌ Network error - no response:", error.message);
        return {
            message: 'Network error. Please check your connection and ensure the server is running at ' + API_CONFIG.BASE_URL,
            status: 0
        };
    } else {
        return {
            message: error.message || 'An unexpected error occurred'
        };
    }
};

export const AuthAPI = {
    async register(data: RegisterRequest): Promise<AuthResponse> {
        try {
            console.log("🚀 Attempting registration...");
            const response = await api.post('/auth/register', data);
            const authData = response.data;

            await TokenManager.setToken(authData.token);

            console.log("✅ Registration successful");
            return authData;
        } catch (error) {
            console.log("❌ Registration failed");
            throw handleApiError(error as AxiosError);
        }
    },

    async login(data: LoginRequest): Promise<AuthResponse> {
        try {
            console.log("🚀 Attempting login...");
            const response = await api.post('/auth/login', data);
            const authData = response.data;

            await TokenManager.setToken(authData.token);

            console.log("✅ Login successful");
            return authData;
        } catch (error) {
            console.log("❌ Login failed");
            throw handleApiError(error as AxiosError);
        }
    },

    async logout(): Promise<void> {
        try {
            await TokenManager.removeToken();
            console.log("✅ Logout successful");
        } catch (error) {
            console.error("❌ Logout error:", error);
        }
    }
};

export default api;