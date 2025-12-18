import React, { createContext, useContext, useEffect, useState, ReactNode } from 'react';
import { AuthAPI } from '../api/api';
import { TokenManager } from '../api/tokenManager';
import { User, LoginRequest, RegisterRequest, ApiError } from '../types/types';

interface AuthContextType {
    user: User | null;
    isLoading: boolean;
    isAuthenticated: boolean;
    isInitialized: boolean;
    login: (data: LoginRequest) => Promise<void>;
    register: (data: RegisterRequest) => Promise<void>;
    logout: () => Promise<void>;
    error: string | null;
    clearError: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

interface AuthProviderProps {
    children: ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
    const [user, setUser] = useState<User | null>(null);
    const [isLoading, setIsLoading] = useState(false);
    const [isInitialized, setIsInitialized] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const isAuthenticated = !!user;

    useEffect(() => {
        checkAuthStatus();
    }, []);

    const checkAuthStatus = async () => {
        try {
            setIsLoading(true);
            const token = await TokenManager.getToken();

            if (token) {
                console.log('🔍 Found existing token');

                if (TokenManager.isTokenExpired(token)) {
                    console.log('🔍 Token expired, removing...');
                    await TokenManager.removeToken();
                } else {
                    const payload = TokenManager.getTokenPayload(token);
                    if (payload) {
                        setUser({
                            id: parseInt(payload.userId || 0),
                            username: payload.username || '',
                            email: payload.email || '',
                            level: 1,
                            xp: 0,
                            totalXP: 0
                        });
                        console.log('✅ User restored from token');
                    }
                }
            } else {
                console.log('🔍 No token found');
            }
        } catch (error) {
            console.log('❌ Token verification failed:', error);
            await TokenManager.removeToken();
            setUser(null);
        } finally {
            setIsLoading(false);
            setIsInitialized(true);
        }
    };

    const login = async (data: LoginRequest) => {
        try {
            setError(null);
            setIsLoading(true);

            console.log('🚀 Attempting login...');
            const response = await AuthAPI.login(data);

            if (!response || !response.user) {
                throw new Error('Invalid response from server');
            }

            setUser(response.user);

            console.log('✅ User logged in successfully');
        } catch (error) {
            console.log('❌ Login failed:', error);
            const err = error as ApiError;
            const errorMessage = err.message || "Login failed. Please check your credentials and try again.";
            setError(errorMessage);
            throw error;
        } finally {
            setIsLoading(false);
        }
    };

    const register = async (data: RegisterRequest) => {
        try {
            setError(null);
            setIsLoading(true);

            console.log('🚀 Attempting registration...');
            const response = await AuthAPI.register(data);

            if (!response || !response.user) {
              throw new Error("Invalid response from server");
            }

            setUser(response.user);

            console.log('✅ User registered successfully');
        } catch (error) {
            console.log('❌ Registration failed:', error);
            const err = error as ApiError;
             const errorMessage = err.message || "Registration failed. Please try again.";
            setError(errorMessage);
            throw error;
        } finally {
            setIsLoading(false);
        }
    };

    const logout = async () => {
        try {
            setIsLoading(true);
            await AuthAPI.logout();
            setUser(null);
            console.log('✅ User logged out');
        } catch (error) {
            console.error('❌ Logout error:', error);
        } finally {
            setIsLoading(false);
        }
    };

    const clearError = () => {
        setError(null);
    };

    const value: AuthContextType = {
        user,
        isLoading,
        isAuthenticated,
        isInitialized,
        login,
        register,
        logout,
        error,
        clearError,
    };

    return (
        <AuthContext.Provider value={value}>
            {children}
        </AuthContext.Provider>
    );
}; 

export const useAuth = (): AuthContextType => {
    const context = useContext(AuthContext);
    if (context === undefined)
        throw new Error('useAuth must be used within an AuthProvider');
    return context;
};