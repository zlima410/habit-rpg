import { useState, useEffect } from 'react';
import NetInfo, { NetInfoState } from '@react-native-community/netinfo';

interface NetworkStatus {
    isConnected: boolean;
    isInternetReachable: boolean | null;
    type: string;
}

export const useNetworkStatus = () => {
    const [networkStatus, setNetworkStatus] = useState<NetworkStatus>({
        isConnected: true,
        isInternetReachable: true,
        type: 'unknown',
    });

    useEffect(() => {
        const unsubscribe = NetInfo.addEventListener((state: NetInfoState) => {
            setNetworkStatus({
                isConnected: state.isConnected ?? false,
                isInternetReachable: state.isInternetReachable,
                type: state.type,
            });
        });

        NetInfo.fetch().then((state: NetInfoState) => {
            setNetworkStatus({
                isConnected: state.isConnected ?? false,
                isInternetReachable: state.isInternetReachable,
                type: state.type,
            });
        });

        return () => {
            unsubscribe();
        };
    }, []);

    return networkStatus;
};