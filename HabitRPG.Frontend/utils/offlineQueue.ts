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

    static async processQueue(): Promise<void> {
        const queue = await this.getQueue();
        if (queue.length === 0) {
            return;
        }

        console.log(`🔄 Processing ${queue.length} queued requests...`);

        const processedIds: string[] = [];
        const remainingQueue: QueuedRequest[] = [];

        for (const request of queue) {
            try {
                if (request.retries >= this.MAX_RETRIES) {
                    console.warn(`⚠️ Skipping request ${request.id} - max retries exceeded`);
                    processedIds.push(request.id);
                    continue;
                }

                const response = await api.request({
                    method: request.method as any,
                    url: request.url,
                    data: request.data,
                    ...request.config,
                });

                console.log(`✅ Processed queued request: ${request.method} ${request.url}`);
                processedIds.push(request.id);
            } catch (error: any) {
                if (!error.response && error.message?.includes('Network')) {
                    request.retries += 1;
                    remainingQueue.push(request);
                    console.log(`⏳ Keeping request ${request.id} in queue (retry ${request.retries}/${this.MAX_RETRIES})`);
                } else {
                    console.error(`❌ Failed to process queued request ${request.id}:`, error);
                    processedIds.push(request.id);
                }
            }
        }

        if (processedIds.length > 0 || remainingQueue.length !== queue.length) {
            await AsyncStorage.setItem(QUEUE_STORAGE_KEY, JSON.stringify(remainingQueue));
        }
    }

    static async clearQueue(): Promise<void> {
        try {
            await AsyncStorage.removeItem(QUEUE_STORAGE_KEY);
        } catch (error) {
            console.error('Error clearing queue:', error);
        }
    }

    static async getQueueSize(): Promise<number> {
        const queue = await this.getQueue();
        return queue.length;
    }
}