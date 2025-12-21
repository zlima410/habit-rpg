import AsyncStorage from "@react-native-async-storage/async-storage";
import axios, { AxiosRequestConfig } from 'axios';
import api from '../api/api';
import { StringToBoolean } from "class-variance-authority/dist/types";

const QUEUE_STORAGE_KEY = 'habitrpg_offline_queue';

interface QueuedRequest {
    id: string;
    method: string;
    url: string;
    data?: any;
    config?: AxiosRequestConfig;
    timestamp: number;
    retries: number;
}

export class OfflineQueue {
    private static readonly MAX_RETRIES = 3;
    private static readonly MAX_QUEUE_SIZE = 100;

    static async enqueue(request: Omit<QueuedRequest, 'id' | 'timestamp' | 'retries'>): Promise<void> {
        try {
            const queue = await this.getQueue();

            if (queue.length >= this.MAX_QUEUE_SIZE) {
                console.warn('Offline queue is full, removing oldest request');
                queue.shift();
            }
        } catch (error) {
            console.error('Error enqueueing request:', error);
        }
    }

    static async getQueue(): Promise<QueuedRequest[]> {
        try {
            const queueStr = await AsyncStorage.getItem(QUEUE_STORAGE_KEY);
            return queueStr ? JSON.parse(queueStr) : [];
        } catch (error) {
            console.error('Error getting queue:', error);
            return [];
        }
    }
}