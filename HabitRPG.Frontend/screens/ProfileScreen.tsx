import React, { useState, useEffect } from "react";
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  Alert,
  RefreshControl,
  TextInput,
  Modal,
  ActivityIndicator,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { User, Settings, LogOut, Crown, Trophy, Flame, Edit3 } from "lucide-react-native";
import PlayerStats from "../components/PlayerStats";
import { colors, spacing, fontSize, borderRadius } from "../constants/theme";
import { useAuth } from "../context/AuthContext";
import { UserProfile, ApiError } from "../types/types";
import api from "../api/api";
import { PlayerStatsSkeleton, CardSkeleton } from "../components/skeletons";

interface UpdateProfileRequest {
  username?: string;
}

export default function ProfileScreen() {
  const { user, logout } = useAuth();
  const [profileData, setProfileData] = useState<UserProfile | null>(null);
  const [refreshing, setRefreshing] = useState(false);
  const [refreshTrigger, setRefreshTrigger] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [isEditingUsername, setIsEditingUsername] = useState(false);
  const [newUsername, setNewUsername] = useState("");
  const [isUpdating, setIsUpdating] = useState(false);

  const fetchProfile = async () => {
    if (!user) {
        setIsLoading(false);
        return;
    }

    try {
      setError(null);
      console.log("🚀 Fetching user profile...");

      const response = await api.get("/user/profile");
      const profile: UserProfile = response.data;

      console.log("✅ Profile loaded successfully");
      setProfileData(profile);
    } catch (error) {
      console.error("❌ Error fetching profile:", error);
      const err = error as ApiError;

      if (!user)
        return;

      setError(err.message || "Failed to load profile data");
    } finally {
      setIsLoading(false);
    }
  };

  const updateUsername = async () => {
    if (!newUsername.trim()) {
      Alert.alert("Error", "Username cannot be empty");
      return;
    }

    if (newUsername.trim() === profileData?.username) {
      setIsEditingUsername(false);
      return;
    }

    if (newUsername.length < 3 || newUsername.length > 50) {
      Alert.alert("Error", "Username must be between 3 and 50 characters");
      return;
    }

    if (!/^[a-zA-Z0-9_-]+$/.test(newUsername)) {
      Alert.alert("Error", "Username can only contain letters, numbers, hyphens, and underscores");
      return;
    }

    try {
      setIsUpdating(true);
      console.log("🚀 Updating username...");

      const updateData: UpdateProfileRequest = {
        username: newUsername.trim(),
      };

      await api.patch("/user/profile", updateData);

      console.log("✅ Username updated successfully");

      await fetchProfile();
      setRefreshTrigger((prev) => prev + 1);

      Alert.alert("Success", "Username updated successfully!");
      setIsEditingUsername(false);
    } catch (err) {
      console.error("❌ Error updating username:", err);
      const apiError = err as ApiError;
      Alert.alert("Error", apiError.message || "Failed to update username");
    } finally {
      setIsUpdating(false);
    }
  };

  const onRefresh = React.useCallback(async () => {
    if (!user) {
        setRefreshing(false);
        return;
    }

    setRefreshing(true);
    await fetchProfile();
    setRefreshTrigger((prev) => prev + 1);
    setRefreshing(false);
  }, [user]);

  const handleLogout = () => {
    Alert.alert("Confirm Logout", "Are you sure you want to sign out?", [
      {
        text: "Cancel",
        style: "cancel",
      },
      {
        text: "Sign Out",
        style: "destructive",
        onPress: async () => {
          try {
            await logout();
          } catch (error) {
            console.error("Logout error:", error);
          }
        },
      },
    ]);
  };

  const handleEditUsername = () => {
    setNewUsername(profileData?.username || "");
    setIsEditingUsername(true);
  };

  const formatJoinDate = (dateString: string) => {
    try {
      const date = new Date(dateString);
      return date.toLocaleDateString("en-US", {
        year: "numeric",
        month: "long",
      });
    } catch {
      return "Unknown";
    }
  };

  useEffect(() => {
    if (user)
      fetchProfile();
    else {
        setProfileData(null);
        setIsLoading(false);
        setError(null);
    }
  }, [user]);

  if (isLoading) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.header}>
          <Text style={styles.title}>Profile</Text>
          <Text style={styles.subtitle}>Manage your character</Text>
        </View>
        <ScrollView style={styles.content} showsVerticalScrollIndicator={false}>
          <PlayerStatsSkeleton />
          <CardSkeleton showHeader={true} showContent={true} showFooter={false} />
          <CardSkeleton showHeader={true} showContent={true} showFooter={false} />
          <View style={styles.bottomSpacing} />
        </ScrollView>
      </SafeAreaView>
    );
  }

  if (error && !profileData) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.errorContainer}>
          <Text style={styles.errorText}>{error}</Text>
          <TouchableOpacity style={styles.retryButton} onPress={fetchProfile}>
            <Text style={styles.retryButtonText}>Retry</Text>
          </TouchableOpacity>
        </View>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>Profile</Text>
        <Text style={styles.subtitle}>Manage your character</Text>
      </View>

      <ScrollView
        style={styles.content}
        showsVerticalScrollIndicator={false}
        refreshControl={
          <RefreshControl
            refreshing={refreshing}
            onRefresh={onRefresh}
            tintColor={colors.primary}
            colors={[colors.primary]}
          />
        }
      >
        <PlayerStats userId={user?.id} refreshTrigger={refreshTrigger} />

        <View style={styles.profileCard}>
          <View style={styles.cardHeader}>
            <User size={20} color={colors.textPrimary} />
            <Text style={styles.cardTitle}>Character Info</Text>
          </View>

          <View style={styles.cardContent}>
            <View style={styles.infoRow}>
              <Text style={styles.infoLabel}>Username</Text>
              <View style={styles.usernameContainer}>
                <Text style={styles.infoValue}>{profileData?.username || user?.username || "Unknown"}</Text>
                <TouchableOpacity style={styles.editButton} onPress={handleEditUsername}>
                  <Edit3 size={16} color={colors.primary} />
                </TouchableOpacity>
              </View>
            </View>

            <View style={styles.infoRow}>
              <Text style={styles.infoLabel}>Email</Text>
              <Text style={styles.infoValue}>{profileData?.email || user?.email || "Unknown"}</Text>
            </View>

            <View style={styles.infoRow}>
              <Text style={styles.infoLabel}>Member Since</Text>
              <Text style={styles.infoValue}>{profileData ? formatJoinDate(profileData.createdAt) : "Unknown"}</Text>
            </View>
          </View>
        </View>

        <View style={styles.profileCard}>
          <View style={styles.cardHeader}>
            <Trophy size={20} color={colors.textPrimary} />
            <Text style={styles.cardTitle}>Statistics</Text>
          </View>

          <View style={styles.cardContent}>
            <View style={styles.statsGrid}>
              <View style={styles.summaryStatItem}>
                <View style={styles.statIconContainer}>
                  <Crown size={24} color={colors.gold} />
                </View>
                <Text style={styles.summaryStatValue}>{profileData?.activeHabitsCount || 0}</Text>
                <Text style={styles.summaryStatLabel}>Active Habits</Text>
              </View>

              <View style={styles.summaryStatItem}>
                <View style={styles.statIconContainer}>
                  <Trophy size={24} color={colors.success} />
                </View>
                <Text style={styles.summaryStatValue}>{profileData?.totalCompletions || 0}</Text>
                <Text style={styles.summaryStatLabel}>Completed</Text>
              </View>

              <View style={styles.summaryStatItem}>
                <View style={styles.statIconContainer}>
                  <Flame size={24} color={colors.fire} />
                </View>
                <Text style={styles.summaryStatValue}>{profileData?.longestStreak || 0}</Text>
                <Text style={styles.summaryStatLabel}>Best Streak</Text>
              </View>
            </View>
          </View>
        </View>

        <View style={styles.profileCard}>
          <View style={styles.cardHeader}>
            <Text style={styles.cardTitle}>Actions</Text>
          </View>

          <View style={styles.cardContent}>
            <TouchableOpacity style={styles.actionButton}>
              <Settings size={20} color={colors.textPrimary} />
              <Text style={styles.actionButtonText}>Settings</Text>
            </TouchableOpacity>

            <TouchableOpacity style={styles.logoutButton} onPress={handleLogout}>
              <LogOut size={20} color={colors.textPrimary} />
              <Text style={styles.logoutButtonText}>Sign Out</Text>
            </TouchableOpacity>
          </View>
        </View>

        <View style={styles.bottomSpacing} />
      </ScrollView>

      <Modal
        visible={isEditingUsername}
        transparent={true}
        animationType="fade"
        onRequestClose={() => setIsEditingUsername(false)}
      >
        <View style={styles.modalOverlay}>
          <View style={styles.modalContent}>
            <Text style={styles.modalTitle}>Edit Username</Text>

            <TextInput
              style={styles.usernameInput}
              value={newUsername}
              onChangeText={setNewUsername}
              placeholder="Enter new username"
              placeholderTextColor={colors.textSecondary}
              autoCapitalize="none"
              maxLength={50}
            />

            <Text style={styles.usernameHint}>
              Username must be 3-50 characters and can only contain letters, numbers, hyphens, and underscores.
            </Text>

            <View style={styles.modalButtons}>
              <TouchableOpacity
                style={[styles.modalButton, styles.cancelButton]}
                onPress={() => setIsEditingUsername(false)}
                disabled={isUpdating}
              >
                <Text style={styles.cancelButtonText}>Cancel</Text>
              </TouchableOpacity>

              <TouchableOpacity
                style={[styles.modalButton, styles.saveButton]}
                onPress={updateUsername}
                disabled={isUpdating}
              >
                {isUpdating ? (
                  <ActivityIndicator size="small" color={colors.textPrimary} />
                ) : (
                  <Text style={styles.saveButtonText}>Save</Text>
                )}
              </TouchableOpacity>
            </View>
          </View>
        </View>
      </Modal>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.background,
  },
  loadingContainer: {
    flex: 1,
    justifyContent: "center",
    alignItems: "center",
  },
  loadingText: {
    color: colors.textSecondary,
    fontSize: fontSize.md,
    marginTop: spacing.sm,
  },
  errorContainer: {
    flex: 1,
    justifyContent: "center",
    alignItems: "center",
    padding: spacing.lg,
  },
  errorText: {
    color: colors.danger,
    fontSize: fontSize.md,
    textAlign: "center",
    marginBottom: spacing.md,
  },
  retryButton: {
    backgroundColor: colors.primary,
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.sm,
    borderRadius: borderRadius.md,
  },
  retryButtonText: {
    color: colors.textPrimary,
    fontSize: fontSize.md,
    fontWeight: "600",
  },
  header: {
    backgroundColor: colors.cardBackground,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    padding: spacing.md,
  },
  title: {
    fontSize: fontSize.xxl,
    fontWeight: "bold",
    color: colors.textPrimary,
  },
  subtitle: {
    fontSize: fontSize.md,
    color: colors.textSecondary,
  },
  content: {
    flex: 1,
    padding: spacing.md,
  },
  profileCard: {
    backgroundColor: colors.cardBackground,
    borderRadius: borderRadius.lg,
    marginBottom: spacing.md,
    borderWidth: 1,
    borderColor: colors.border,
  },
  cardHeader: {
    flexDirection: "row",
    alignItems: "center",
    padding: spacing.md,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  cardTitle: {
    fontSize: fontSize.lg,
    fontWeight: "600",
    color: colors.textPrimary,
    marginLeft: spacing.sm,
  },
  cardContent: {
    padding: spacing.md,
  },
  infoRow: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    paddingVertical: spacing.sm,
  },
  infoLabel: {
    fontSize: fontSize.md,
    color: colors.textSecondary,
  },
  infoValue: {
    fontSize: fontSize.md,
    color: colors.textPrimary,
    fontWeight: "500",
  },
  usernameContainer: {
    flexDirection: "row",
    alignItems: "center",
  },
  editButton: {
    marginLeft: spacing.sm,
    padding: spacing.xs,
  },
  statsGrid: {
    flexDirection: "row",
    justifyContent: "space-around",
  },
  summaryStatItem: {
    alignItems: "center",
    flex: 1,
  },
  statIconContainer: {
    marginBottom: spacing.sm,
  },
  summaryStatValue: {
    fontSize: fontSize.lg,
    fontWeight: "bold",
    color: colors.textPrimary,
  },
  summaryStatLabel: {
    fontSize: fontSize.xs,
    color: colors.textSecondary,
    marginTop: spacing.xs,
    textAlign: "center",
  },
  actionButton: {
    flexDirection: "row",
    alignItems: "center",
    paddingVertical: spacing.md,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  actionButtonText: {
    fontSize: fontSize.md,
    color: colors.textPrimary,
    marginLeft: spacing.sm,
  },
  logoutButton: {
    flexDirection: "row",
    alignItems: "center",
    paddingVertical: spacing.md,
  },
  logoutButtonText: {
    fontSize: fontSize.md,
    color: colors.danger,
    marginLeft: spacing.sm,
    fontWeight: "500",
  },
  bottomSpacing: {
    height: spacing.xl,
  },
  modalOverlay: {
    flex: 1,
    backgroundColor: "rgba(0, 0, 0, 0.5)",
    justifyContent: "center",
    alignItems: "center",
    padding: spacing.lg,
  },
  modalContent: {
    backgroundColor: colors.cardBackground,
    borderRadius: borderRadius.lg,
    padding: spacing.lg,
    width: "100%",
    maxWidth: 400,
  },
  modalTitle: {
    fontSize: fontSize.xl,
    fontWeight: "bold",
    color: colors.textPrimary,
    marginBottom: spacing.md,
    textAlign: "center",
  },
  usernameInput: {
    backgroundColor: colors.background,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: borderRadius.md,
    padding: spacing.md,
    fontSize: fontSize.md,
    color: colors.textPrimary,
    marginBottom: spacing.sm,
  },
  usernameHint: {
    fontSize: fontSize.sm,
    color: colors.textSecondary,
    marginBottom: spacing.lg,
  },
  modalButtons: {
    flexDirection: "row",
    justifyContent: "space-between",
    gap: spacing.md,
  },
  modalButton: {
    flex: 1,
    paddingVertical: spacing.md,
    borderRadius: borderRadius.md,
    alignItems: "center",
  },
  cancelButton: {
    backgroundColor: colors.border,
  },
  cancelButtonText: {
    color: colors.textPrimary,
    fontSize: fontSize.md,
    fontWeight: "600",
  },
  saveButton: {
    backgroundColor: colors.primary,
  },
  saveButtonText: {
    color: colors.textPrimary,
    fontSize: fontSize.md,
    fontWeight: "600",
  },
});
