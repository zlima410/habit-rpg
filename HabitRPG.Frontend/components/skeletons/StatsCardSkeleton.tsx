import React from "react";
import { View, StyleSheet } from "react-native";
import Skeleton from "./Skeleton";
import { colors, spacing, borderRadius } from "../../constants/theme";

const StatsCardSkeleton: React.FC = () => {
  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Skeleton width={20} height={20} borderRadius={10} />
        <Skeleton width={120} height={18} borderRadius={4} style={styles.headerTitle} />
      </View>

      <View style={styles.content}>
        <View style={styles.progressSection}>
          <View style={styles.progressHeader}>
            <Skeleton width={80} height={14} borderRadius={4} />
            <Skeleton width={40} height={14} borderRadius={4} />
          </View>
          <Skeleton width="100%" height={8} borderRadius={4} style={styles.progressBar} />
        </View>

        <View style={styles.progressSection}>
          <View style={styles.progressHeader}>
            <Skeleton width={80} height={14} borderRadius={4} />
            <Skeleton width={40} height={14} borderRadius={4} />
          </View>
          <Skeleton width="100%" height={8} borderRadius={4} style={styles.progressBar} />
        </View>
      </View>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    backgroundColor: colors.cardBackground,
    borderRadius: borderRadius.lg,
    marginBottom: spacing.md,
    borderWidth: 1,
    borderColor: colors.border,
    overflow: "hidden",
  },
  header: {
    flexDirection: "row",
    alignItems: "center",
    padding: spacing.md,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  headerTitle: {
    marginLeft: spacing.sm,
  },
  content: {
    padding: spacing.md,
  },
  progressSection: {
    marginBottom: spacing.lg,
  },
  progressHeader: {
    flexDirection: "row",
    justifyContent: "space-between",
    marginBottom: spacing.sm,
  },
  progressBar: {
    marginTop: spacing.xs,
  },
});

export default StatsCardSkeleton;