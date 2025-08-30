import React, { useState } from "react";
import {
  View,
  Text,
  StyleSheet,
  Modal,
  TextInput,
  TouchableOpacity,
  Alert,
  ActivityIndicator,
  ScrollView,
} from "react-native";
import { X, Plus } from "lucide-react-native";
import { colors, spacing, fontSize, borderRadius } from "../constants/theme";
import { CreateHabitRequest, Habit, HabitFrequency, HabitDifficulty, ApiError } from "../types/types";
import api from "../api/api";

interface CreateHabitModalProps {
  visible: boolean;
  onClose: () => void;
  onSuccess: (newHabit: Habit) => void;
}

export default function CreateHabitModal({ visible, onClose, onSuccess }: CreateHabitModalProps) {
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [frequency, setFrequency] = useState<HabitFrequency>(HabitFrequency.Daily);
  const [difficulty, setDifficulty] = useState<HabitDifficulty>(HabitDifficulty.Medium);
  const [isCreating, setIsCreating] = useState(false);

  const frequencyOptions = [
    { value: HabitFrequency.Daily, label: "Daily", description: "Complete every day" },
    { value: HabitFrequency.Weekly, label: "Weekly", description: "Complete once per week" },
  ];

  const difficultyOptions = [
    {
      value: HabitDifficulty.Easy,
      label: "Easy",
      xp: 5,
      color: colors.success,
      description: "Simple tasks, +5 XP",
    },
    {
      value: HabitDifficulty.Medium,
      label: "Medium",
      xp: 10,
      color: colors.warning,
      description: "Moderate effort, +10 XP",
    },
    {
      value: HabitDifficulty.Hard,
      label: "Hard",
      xp: 20,
      color: colors.danger,
      description: "Challenging tasks, +20 XP",
    },
  ];

  const resetForm = () => {
    setTitle("");
    setDescription("");
    setFrequency(HabitFrequency.Daily);
    setDifficulty(HabitDifficulty.Medium);
  };

  const validateForm = () => {
    if (!title.trim()) {
      Alert.alert("Validation Error", "Habit title is required");
      return false;
    }

    if (title.trim().length < 1 || title.trim().length > 200) {
      Alert.alert("Validation Error", "Habit title must be between 1 and 200 characters");
      return false;
    }

    if (description.length > 1000) {
      Alert.alert("Validation Error", "Description cannot exceed 1000 characters");
      return false;
    }

    return true;
  };

  const handleCreate = async () => {
    if (!validateForm()) return;

    try {
      setIsCreating(true);
      console.log("🚀 Creating new habit...");

      const createRequest: CreateHabitRequest = {
        title: title.trim(),
        description: description.trim() || undefined,
        frequency,
        difficulty,
      };

      const response = await api.post("/habits", createRequest);
      const newHabit: Habit = response.data;

      console.log("✅ Habit created successfully:", newHabit.title);

      Alert.alert("Success!", `Habit "${newHabit.title}" has been created successfully!`, [
        {
          text: "Great!",
          onPress: () => {
            onSuccess(newHabit);
            handleClose();
          },
        },
      ]);
    } catch (err) {
      console.error("❌ Error creating habit:", err);
      const error = err as ApiError;

      let errorMessage = error.message || "Failed to create habit";

      if (error.status === 400) {
        if (error.message.includes("already exists")) {
          errorMessage = "A habit with this title already exists";
        } else if (error.message.includes("maximum")) {
          errorMessage = "You've reached the maximum number of habits (100)";
        }
      }

      Alert.alert("Error", errorMessage);
    } finally {
      setIsCreating(false);
    }
  };

  const handleClose = () => {
    if (isCreating) return;
    resetForm();
    onClose();
  };

  const renderFrequencyOption = (option: (typeof frequencyOptions)[0]) => (
    <TouchableOpacity
      key={option.value}
      style={[styles.optionButton, frequency === option.value && styles.optionButtonSelected]}
      onPress={() => setFrequency(option.value)}
    >
      <Text style={[styles.optionLabel, frequency === option.value && styles.optionLabelSelected]}>{option.label}</Text>
      <Text style={[styles.optionDescription, frequency === option.value && styles.optionDescriptionSelected]}>
        {option.description}
      </Text>
    </TouchableOpacity>
  );

  const renderDifficultyOption = (option: (typeof difficultyOptions)[0]) => (
    <TouchableOpacity
      key={option.value}
      style={[
        styles.optionButton,
        difficulty === option.value && styles.optionButtonSelected,
        difficulty === option.value && { borderColor: option.color },
      ]}
      onPress={() => setDifficulty(option.value)}
    >
      <View style={styles.difficultyHeader}>
        <Text style={[styles.optionLabel, difficulty === option.value && styles.optionLabelSelected]}>
          {option.label}
        </Text>
        <View style={[styles.xpBadge, { backgroundColor: `${option.color}20` }]}>
          <Text style={[styles.xpText, { color: option.color }]}>+{option.xp} XP</Text>
        </View>
      </View>
      <Text style={[styles.optionDescription, difficulty === option.value && styles.optionDescriptionSelected]}>
        {option.description}
      </Text>
    </TouchableOpacity>
  );

  return (
    <Modal visible={visible} transparent={true} animationType="fade" onRequestClose={handleClose}>
      <View style={styles.modalOverlay}>
        <View style={styles.modalContent}>
          <ScrollView showsVerticalScrollIndicator={false}>
            <View style={styles.modalHeader}>
              <Text style={styles.modalTitle}>Create New Habit</Text>
              <TouchableOpacity style={styles.closeButton} onPress={handleClose} disabled={isCreating}>
                <X size={24} color={colors.textSecondary} />
              </TouchableOpacity>
            </View>

            <View style={styles.form}>
              <View style={styles.fieldGroup}>
                <Text style={styles.fieldLabel}>Habit Title *</Text>
                <TextInput
                  style={styles.textInput}
                  value={title}
                  onChangeText={setTitle}
                  placeholder="e.g., Morning Meditation, Read 30 minutes"
                  placeholderTextColor={colors.textSecondary}
                  maxLength={200}
                  editable={!isCreating}
                />
                <Text style={styles.fieldHint}>{title.length}/200 characters</Text>
              </View>

              <View style={styles.fieldGroup}>
                <Text style={styles.fieldLabel}>Description (Optional)</Text>
                <TextInput
                  style={[styles.textInput, styles.multilineInput]}
                  value={description}
                  onChangeText={setDescription}
                  placeholder="Add details about your habit..."
                  placeholderTextColor={colors.textSecondary}
                  multiline={true}
                  numberOfLines={3}
                  maxLength={1000}
                  textAlignVertical="top"
                  editable={!isCreating}
                />
                <Text style={styles.fieldHint}>{description.length}/1000 characters</Text>
              </View>

              <View style={styles.fieldGroup}>
                <Text style={styles.fieldLabel}>Frequency *</Text>
                <View style={styles.optionsContainer}>{frequencyOptions.map(renderFrequencyOption)}</View>
              </View>

              <View style={styles.fieldGroup}>
                <Text style={styles.fieldLabel}>Difficulty *</Text>
                <View style={styles.optionsContainer}>{difficultyOptions.map(renderDifficultyOption)}</View>
              </View>
            </View>

            <View style={styles.modalActions}>
              <TouchableOpacity
                style={[styles.actionButton, styles.cancelButton]}
                onPress={handleClose}
                disabled={isCreating}
              >
                <Text style={styles.cancelButtonText}>Cancel</Text>
              </TouchableOpacity>

              <TouchableOpacity
                style={[styles.actionButton, styles.createButton, isCreating && styles.createButtonDisabled]}
                onPress={handleCreate}
                disabled={isCreating || !title.trim()}
              >
                {isCreating ? (
                  <ActivityIndicator size="small" color={colors.textPrimary} />
                ) : (
                  <>
                    <Plus size={18} color={colors.textPrimary} />
                    <Text style={styles.createButtonText}>Create Habit</Text>
                  </>
                )}
              </TouchableOpacity>
            </View>
          </ScrollView>
        </View>
      </View>
    </Modal>
  );
}

const styles = StyleSheet.create({
  modalOverlay: {
    flex: 1,
    backgroundColor: "rgba(0, 0, 0, 0.5)",
    justifyContent: "center",
    alignItems: "center",
    padding: spacing.md,
  },
  modalContent: {
    backgroundColor: colors.cardBackground,
    borderRadius: borderRadius.lg,
    width: "100%",
    maxWidth: 400,
    maxHeight: "90%",
  },
  modalHeader: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    padding: spacing.lg,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  modalTitle: {
    fontSize: fontSize.xl,
    fontWeight: "bold",
    color: colors.textPrimary,
  },
  closeButton: {
    padding: spacing.xs,
  },
  form: {
    padding: spacing.lg,
  },
  fieldGroup: {
    marginBottom: spacing.lg,
  },
  fieldLabel: {
    fontSize: fontSize.md,
    fontWeight: "600",
    color: colors.textPrimary,
    marginBottom: spacing.sm,
  },
  textInput: {
    backgroundColor: colors.background,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: borderRadius.md,
    padding: spacing.md,
    fontSize: fontSize.md,
    color: colors.textPrimary,
    minHeight: 48,
  },
  multilineInput: {
    minHeight: 80,
    textAlignVertical: "top",
  },
  fieldHint: {
    fontSize: fontSize.xs,
    color: colors.textSecondary,
    marginTop: spacing.xs,
    textAlign: "right",
  },
  optionsContainer: {
    gap: spacing.sm,
  },
  optionButton: {
    backgroundColor: colors.background,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: borderRadius.md,
    padding: spacing.md,
  },
  optionButtonSelected: {
    borderColor: colors.primary,
    backgroundColor: `${colors.primary}10`,
  },
  optionLabel: {
    fontSize: fontSize.md,
    fontWeight: "600",
    color: colors.textPrimary,
    marginBottom: spacing.xs,
  },
  optionLabelSelected: {
    color: colors.primary,
  },
  optionDescription: {
    fontSize: fontSize.sm,
    color: colors.textSecondary,
  },
  optionDescriptionSelected: {
    color: colors.textPrimary,
  },
  difficultyHeader: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: spacing.xs,
  },
  xpBadge: {
    paddingHorizontal: spacing.sm,
    paddingVertical: spacing.xs / 2,
    borderRadius: borderRadius.sm,
  },
  xpText: {
    fontSize: fontSize.xs,
    fontWeight: "600",
  },
  modalActions: {
    flexDirection: "row",
    justifyContent: "space-between",
    padding: spacing.lg,
    borderTopWidth: 1,
    borderTopColor: colors.border,
    gap: spacing.md,
  },
  actionButton: {
    flex: 1,
    paddingVertical: spacing.md,
    borderRadius: borderRadius.md,
    alignItems: "center",
    justifyContent: "center",
    minHeight: 48,
  },
  cancelButton: {
    backgroundColor: colors.background,
    borderWidth: 1,
    borderColor: colors.border,
  },
  cancelButtonText: {
    color: colors.textPrimary,
    fontSize: fontSize.md,
    fontWeight: "600",
  },
  createButton: {
    backgroundColor: colors.primary,
    flexDirection: "row",
    alignItems: "center",
  },
  createButtonDisabled: {
    opacity: 0.6,
  },
  createButtonText: {
    color: colors.textPrimary,
    fontSize: fontSize.md,
    fontWeight: "600",
    marginLeft: spacing.xs,
  },
});
