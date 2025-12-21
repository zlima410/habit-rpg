import AsyncStorage from '@react-native-async-storage/async-storage';

const STORAGE_PREFIX = 'habitrpg_cache_';
const CACHE_EXPIRY_PREFIX = 'habitrpg_cache_expiry_';
const DEFAULT_CACHE_TTL = 5 * 60 * 1000;

interface CacheEntry<T> {
    data: T;
    timestamp: number;
    expiry: number;
}

export class OfflineStorage {
    static async set<T>(key: string, data: T, ttl: number = DEFAULT_CACHE_TTL): Promise<void> {
        try {
            const cacheKey = `${STORAGE_PREFIX}${key}`;
            const expiryKey = `${CACHE_EXPIRY_PREFIX}${key}`;
            const timestamp = Date.now();
            const expiry = timestamp + ttl;

            const entry: CacheEntry<T> = {
                data,
                timestamp,
                expiry,
            };

            await AsyncStorage.setItem(cacheKey, JSON.stringify(entry));
            await AsyncStorage.setItem(expiryKey, expiry.toString());
        } catch (error) {
            console.error(`Error caching data for key ${key}:`, error);
        }
    }

    static async get<T>(key: string): Promise<T | null> {
        try {
            const cacheKey = `${STORAGE_PREFIX}${key}`;
            const expiryKey = `${CACHE_EXPIRY_PREFIX}${key}`;

            const [cachedData, expiryStr] = await Promise.all([
                AsyncStorage.getItem(cacheKey),
                AsyncStorage.getItem(expiryKey),
            ]);

            if (!cachedData || !expiryStr) {
                return null;
            }

            const expiry = parseInt(expiryStr, 10);
            if (Date.now() > expiry) {
                await Promise.all([
                    AsyncStorage.removeItem(cacheKey),
                    AsyncStorage.removeItem(expiryKey),
                ]);
                return null;
            }

            const entry: CacheEntry<T> = JSON.parse(cachedData);
            return entry.data;
        } catch (error) {
            console.error(`Error retrieving cached data for key ${key}:`, error);
            return null;
        }
    }

    static async remove(key: string): Promise<void> {
        try {
            const cacheKey = `${STORAGE_PREFIX}${key}`;
            const expiryKey = `${CACHE_EXPIRY_PREFIX}${key}`;
            await Promise.all([
                AsyncStorage.removeItem(cacheKey),
                AsyncStorage.removeItem(expiryKey),
            ]);
        } catch (error) {
            console.error(`Error removing cached data for key ${key}:`, error);
        }
    }

    static async clear(): Promise<void> {
        try {
            const keys = await AsyncStorage.getAllKeys();
            const cacheKeys = keys.filter(
                (key) => key.startsWith(STORAGE_PREFIX) || key.startsWith(CACHE_EXPIRY_PREFIX)
            );
            await AsyncStorage.multiRemove(cacheKeys);
        } catch (error) {
            console.error('Error clearing cache:', error);
        }
    }

    static async getAllKeys(): Promise<string[]> {
        try {
            const keys = await AsyncStorage.getAllKeys();
            return keys
                .filter((key) => key.startsWith(STORAGE_PREFIX))
                .map((key) => key.replace(STORAGE_PREFIX, ''));
        } catch (error) {
            console.error('Error getting cache keys:', error);
            return [];
        }
    }
}