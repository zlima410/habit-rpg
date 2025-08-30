import React, { createContext, useContext, useState, useCallback, ReactNode } from "react";
import { Habit, GameReward } from "../types/types";

interface HabitsContextType {
  refreshTrigger: number;
  triggerRefresh: () => void;
  onHabitCompleted: (habitId: number, reward: GameReward) => void;
  onHabitCreated: (newHabit: Habit) => void;
  onHabitUpdated: (updatedHabit: Habit) => void;
}

const HabitsContext = createContext<HabitsContextType | undefined>(undefined);

interface HabitsProviderProps {
  children: ReactNode;
}

export const HabitsProvider: React.FC<HabitsProviderProps> = ({ children }) => {
  const [refreshTrigger, setRefreshTrigger] = useState(0);

  const triggerRefresh = useCallback(() => {
    console.log("🔄 Triggering global habits refresh");
    setRefreshTrigger((prev) => prev + 1);
  }, []);

  const onHabitCompleted = useCallback(
    (habitId: number, reward: GameReward) => {
      console.log(`✅ Global habit completion: Habit ${habitId} completed, +${reward.xpGained} XP`);
      triggerRefresh();
    },
    [triggerRefresh]
  );

  const onHabitCreated = useCallback(
    (newHabit: Habit) => {
      console.log(`✅ Global habit creation: "${newHabit.title}" created`);
      triggerRefresh();
    },
    [triggerRefresh]
  );

  const onHabitUpdated = useCallback(
    (updatedHabit: Habit) => {
      console.log(`✅ Global habit update: "${updatedHabit.title}" updated`);
      triggerRefresh();
    },
    [triggerRefresh]
  );

  const value: HabitsContextType = {
    refreshTrigger,
    triggerRefresh,
    onHabitCompleted,
    onHabitCreated,
    onHabitUpdated,
  };

  return <HabitsContext.Provider value={value}>{children}</HabitsContext.Provider>;
};

export const useHabits = (): HabitsContextType => {
  const context = useContext(HabitsContext);
  if (context === undefined) {
    throw new Error("useHabits must be used within a HabitsProvider");
  }
  return context;
};
