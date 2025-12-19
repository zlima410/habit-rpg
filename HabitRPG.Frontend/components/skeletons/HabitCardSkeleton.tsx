import React from 'react';
import { View, StyleSheet } from 'react-native';
import Skeleton from './Skeleton';
import { colors, spacing, borderRadius } from '../../constants/theme';

const HabitCardSkeleton: React.FC = () => {
    return (
        <View style={styles.container}>
            <View style={styles.content}>
                <View style={styles.checkButtonContainer}>
                    <Skeleton width={24} height={24} borderRadius={12} />
                </View>

                <View style={styles.habitInfo}>
                    <View style={styles.titleRow}>
                        <Skeleton width="60%" height={18} borderRadius={4} />
                        <Skeleton width={50} height={20} borderRadius={4} style={styles.badge} />
                    </View>

                    <Skeleton width="80%" height={14} borderRadius={4} style={styles.description} />

                    <View style={styles.statsRow}>
                        <Skeleton width={60} height={14} borderRadius={4} />
                        <Skeleton width={80} height={14} borderRadius={4} style={styles.statSpacing} />
                    </View>
                </View>
            </View>
        </View>
    );
};

const styles = StyleSheet.create({
  container: {
    backgroundColor: colors.cardBackground,
    borderRadius: borderRadius.lg,
    marginBottom: spacing.sm,
    borderWidth: 1,
    borderColor: colors.border,
    overflow: "hidden",
  },
  content: {
    padding: spacing.md,
    flexDirection: "row",
    alignItems: "flex-start",
  },
  checkButtonContainer: {
    marginRight: spacing.md,
    padding: spacing.xs,
    justifyContent: "center",
    alignItems: "center",
    minWidth: 32,
    minHeight: 32,
  },
  habitInfo: {
    flex: 1,
  },
  titleRow: {
    flexDirection: "row",
    alignItems: "center",
    marginBottom: spacing.xs,
    gap: spacing.sm,
  },
  badge: {
    marginLeft: "auto",
  },
  description: {
    marginBottom: spacing.sm,
  },
  statsRow: {
    flexDirection: "row",
    alignItems: "center",
    marginTop: spacing.xs,
  },
  statSpacing: {
    marginLeft: spacing.lg,
  },
});

export default HabitCardSkeleton;