export const CacheKeys = {
    habits: 'habits',
    activeHabits: 'habits_active',
    inactiveHabits: 'habits_inactive',
    userProfile: (userId: number) => `user_profile_${userId}`,
    userStats: (userId: number, days: number) => `user_stats_${userId}_${days}`,
    todayHabits: 'habits_today',
} as const;