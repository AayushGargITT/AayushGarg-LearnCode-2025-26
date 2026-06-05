import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Allocation } from '../models/allocation.model';

@Injectable({ providedIn: 'root' })
export class AllocationService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'https://localhost:44374/api/v1/allocation';

  getAllAllocations(): Observable<Allocation[]> {
    return this.http.get<Allocation[]>(this.apiUrl);
  }
}
