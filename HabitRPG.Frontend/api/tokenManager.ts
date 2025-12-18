import * as SecureStore from "expo-secure-store";
import { STORAGE_KEYS } from '../config';
import { Platform } from "react-native";

export class TokenManager {
    private static readonly TOKEN_KEY = STORAGE_KEYS.AUTH_TOKEN;

    static async setToken(token: string): Promise<void> {
        try {
            if (!token || typeof token !== 'string')
                throw new Error('Invalid token provided');

            await SecureStore.setItemAsync(this.TOKEN_KEY, token, {
              keychainService: "habitrpg_keychain",
              requireAuthentication: false,
              keychainAccessible: SecureStore.WHEN_UNLOCKED_THIS_DEVICE_ONLY,
            });

            console.log('🔐 Token stored securely');
        } catch (error) {
            console.error('❌ Error storing token:', error);
            console.warn("⚠️ Continuing without secure token storage");
        }
    }

    static async getToken(): Promise<string | null> {
        try {
            const token = await SecureStore.getItemAsync(this.TOKEN_KEY, {
              keychainService: "habitrpg_keychain",
              requireAuthentication: true,
              keychainAccessible: SecureStore.WHEN_UNLOCKED_THIS_DEVICE_ONLY,
            });

            if (token) {
                if (!this.isValidJWTFormat(token)) {
                    console.warn('⚠️ Invalid token format found, removing...');
                    await this.removeToken();
                    return null;
                }

                console.log('🔐 Token retrieved securely');
            }
            
            return token;
        } catch (error) {
            console.error("❌ Error retrieving token:", error);
            return null;
        }
    }

    static async removeToken(): Promise<void> {
        try {
            await SecureStore.deleteItemAsync(this.TOKEN_KEY, {
                keychainService: 'habitrpg_keychain',
            });

            console.log("🔐 Token removed securely");
        } catch (error) {
            console.error("❌ Error removing token:", error);
        }
    }

    static async hasToken(): Promise<boolean> {
        try {
            const token = await this.getToken();
            return token !== null;
        } catch (error) {
            return false;
        }
    }

    static getTokenPayload(token: string): any | null {
        try {
            const parts = token.split('.');
            if (parts.length !== 3) return null;

            const payload = JSON.parse(atob(parts[1]));
            return payload;
        } catch {
            return null;
        }
    }

    private static isValidJWTFormat(token: string): boolean {
        try {
            const parts = token.split('.');
            return parts.length === 3;
        } catch {
            return false;
        }
    }

    static isTokenExpired(token: string): boolean {
        try {
            const payload = this.getTokenPayload(token);
            if (!payload || !payload.exp) return true;

            const now = Math.floor(Date.now() / 1000);
            return payload.exp < now;
        } catch {
            return true;
        }
    }
}
