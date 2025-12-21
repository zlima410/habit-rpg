import React, { useEffect, useState } from 'react';
import { View, Text, StyleSheet, Animated } from 'react-native';
import { WifiOff } from 'lucide-react-native';
import { useNetworkStatus } from '../hooks/useNetworkStatus';
import { colors, spacing, fontSize, borderRadius } from '../constants/theme';

const OfflineIndicator: React.FC = () => {
    const { isConnected, isInternetReachable } = useNetworkStatus();
    const [showIndicator, setShowIndicator] = useState(false);
    const slideAnim = React.useRef(new Animated.Value(-100)).current;

    const isOffline = !isConnected || isInternetReachable === false;

    useEffect(() => {
        if (isOffline) {
            setShowIndicator(true);
            Animated.spring(slideAnim, {
                toValue: 0,
                useNativeDriver: true,
                tension: 50,
                friction: 8,
            }).start();
        } else {
            Animated.timing(slideAnim, {
                toValue: -100,
                duration: 300,
                useNativeDriver: true,
            }).start(() => {
                setShowIndicator(false);
            });
        }
    }, [isOffline, slideAnim]);

    if (!showIndicator) {
        return null;
    }

    return (
        <Animated.View
            style={[
                styles.container,
                {
                    transform: [{ translateY: slideAnim }],
                },
            ]}
        >
            <View style={styles.content}>
                <WifiOff size={16} color={colors.textPrimary} />
                <Text style={styles.text}>No internet connection</Text>
            </View>
        </Animated.View>
    );
};

const styles = StyleSheet.create({
    container: {
        position: 'absolute',
        top: 0,
        left: 0,
        right: 0,
        backgroundColor: colors.danger,
        paddingVertical: spacing.sm,
        paddingHorizontal: spacing.md,
        zIndex: 9999,
        elevation: 5,
        shadowColor: '#000',
        shadowOffset: { width: 0, height: 2 },
        shadowOpacity: 0.25,
        shadowRadius: 3.84,
    },
    content: {
        flexDirection: 'row',
        alignItems: 'center',
        justifyContent: 'center',
        gap: spacing.sm,
    },
    text: {
        color: colors.textPrimary,
        fontSize: fontSize.sm,
        fontWeight: '600',
    },
});

export default OfflineIndicator;