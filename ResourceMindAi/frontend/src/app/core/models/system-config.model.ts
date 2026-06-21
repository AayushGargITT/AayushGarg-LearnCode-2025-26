export interface SystemConfig {
  id: string;
  llmProvider: string;
  llmApiKey: string;
  schedulerIntervalHours: number;
  maxWeeklyHours: number;
}
