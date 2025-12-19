import React from "react";
import { View, StyleSheet } from "react-native";
import Skeleton from "./Skeleton";
import { spacing, borderRadius } from "../../constants/theme";

const StatBoxSkeleton: React.FC = () => {
  return (
    <View style={styles.container}>
      <Skeleton width={40} height={40} borderRadius={20} />
      <Skeleton width={60} height={24} borderRadius={4} style={styles.value} />
      <Skeleton width={50} height={12} borderRadius={4} />
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    alignItems: "center",
    padding: spacing.lg,
    borderRadius: borderRadius.md,
  },
  value: {
    marginTop: spacing.sm,
    marginBottom: spacing.xs,
  },
});

export default StatBoxSkeleton;