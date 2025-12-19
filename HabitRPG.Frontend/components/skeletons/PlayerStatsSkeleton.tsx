import React from 'react';
import { View, StyleSheet } from 'react-native';
import Skeleton from './Skeleton';
import { colors, spacing, borderRadius } from '../../constants/theme';

const PlayerStatsSkeleton: React.FC = () => {
    return (
      <View style={styles.container}>
        <View style={styles.levelSection}>
          <Skeleton width={80} height={80} borderRadius={40} />
          <View style={styles.levelInfo}>
            <Skeleton width={100} height={24} borderRadius={4} style={styles.levelText} />
            <Skeleton width={60} height={16} borderRadius={4} />
          </View>
        </View>

        <View style={styles.xpSection}>
          <View style={styles.xpHeader}>
            <Skeleton width={80} height={14} borderRadius={4} />
            <Skeleton width={40} height={14} borderRadius={4} />
          </View>
          <Skeleton width="100%" height={8} borderRadius={4} style={styles.progressBar} />
        </View>

        <View style={styles.statsGrid}>
          {Array.from({ length: 4 }).map((_, index) => (
            <View key={index} style={styles.statItem}>
              <Skeleton width={40} height={40} borderRadius={20} />
              <Skeleton width={60} height={14} borderRadius={4} style={styles.statValue} />
              <Skeleton width={50} height={12} borderRadius={4} />
            </View>
          ))}
        </View>
      </View>
    );
};

const styles = StyleSheet.create({
  container: {
    backgroundColor: colors.cardBackground,
    borderRadius: borderRadius.lg,
    padding: spacing.md,
    borderWidth: 1,
    borderColor: colors.border,
  },
  levelSection: {
    flexDirection: "row",
    alignItems: "center",
    marginBottom: spacing.md,
  },
  levelInfo: {
    marginLeft: spacing.md,
    flex: 1,
  },
  levelText: {
    marginBottom: spacing.xs,
  },
  xpSection: {
    marginBottom: spacing.md,
  },
  xpHeader: {
    flexDirection: "row",
    justifyContent: "space-between",
    marginBottom: spacing.sm,
  },
  progressBar: {
    marginTop: spacing.xs,
  },
  statsGrid: {
    flexDirection: "row",
    flexWrap: "wrap",
    justifyContent: "space-between",
  },
  statItem: {
    width: "48%",
    alignItems: "center",
    marginBottom: spacing.md,
  },
  statValue: {
    marginTop: spacing.sm,
    marginBottom: spacing.xs,
  },
});

export default PlayerStatsSkeleton;