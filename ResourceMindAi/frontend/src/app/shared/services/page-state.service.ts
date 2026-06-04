import { Injectable, signal } from '@angular/core';

@Injectable()
export class PageStateService {
  readonly isLoading = signal(false);
  readonly isSubmitting = signal(false);
  readonly successMessage = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly activeActionId = signal<string | null>(null);

  startLoading(): void {
    this.isLoading.set(true);
  }

  stopLoading(): void {
    this.isLoading.set(false);
  }

  startSubmitting(): void {
    this.isSubmitting.set(true);
  }

  stopSubmitting(): void {
    this.isSubmitting.set(false);
  }

  startAction(id: string): void {
    this.activeActionId.set(id);
  }

  stopAction(): void {
    this.activeActionId.set(null);
  }

  setSuccess(message: string): void {
    this.successMessage.set(message);
    this.errorMessage.set(null);
  }

  setError(message: string): void {
    this.errorMessage.set(message);
  }

  clearFeedback(): void {
    this.successMessage.set(null);
    this.errorMessage.set(null);
  }
}
