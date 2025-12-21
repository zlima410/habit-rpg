import { useEffect, useState, useCallback } from 'react';
import { useNetworkStatus } from './useNetworkStatus';
import { OfflineQueue } from '../utils/offlineQueue';

export const useOfflineSync = () => {
    const { isConnected, isInternetReachable } = useNetworkStatus();
    const [isSyncing, setIsSyncing] = useState(false);
    const [queueSize, setQueueSize] = useState(0);

    const isOnline = isConnected && isInternetReachable !== false;

    const syncQueue = useCallback(async () => {
        if (!isOnline || isSyncing) {
            return;
        }

        try {
            setIsSyncing(true);
            const size = await OfflineQueue.getQueueSize();

            if (size > 0) {
                console.log(`🔄 Syncing ${size} queued requests...`);
                await OfflineQueue.processQueue();
                const newSize = await OfflineQueue.getQueueSize();
                setQueueSize(newSize);
                console.log(`✅ Sync complete. ${newSize} requests remaining.`);
            } else {
                setQueueSize(0);
            }
        } catch (error) {
            console.error('Error syncing queue:', error);
        } finally {
            setIsSyncing(false);
        }
    }, [isOnline, isSyncing]);

    useEffect(() => {
        if (isOnline) {
            syncQueue();
        }

        const interval = setInterval(async () => {
            const size = await OfflineQueue.getQueueSize();
            setQueueSize(size);
        }, 5000);

        return () => clearInterval(interval);
    }, [isOnline, syncQueue]);

    return {
        isSyncing,
        queueSize,
        syncQueue,
        isOnline,
    };
};