import React from 'react';
import { View, StyleSheet } from 'react-native';
import HabitCardSkeleton from './HabitCardSkeleton';
import { spacing } from '../../constants/theme';

interface HabitListSkeletonProps {
    count?: number;
}

const HabitListSkeleton: React.FC<HabitListSkeletonProps> = ({ count = 3 }) => {
    return (
        <View style={styles.container}>
            {Array.from({ length: count }).map((_, index) => (
                <HabitCardSkeleton key={index} />
            ))}
        </View>
    );
};

const styles = StyleSheet.create({
    container: {
        paddingHorizontal: spacing.md,
        paddingTop: spacing.sm,
    },
});

export default HabitListSkeleton;