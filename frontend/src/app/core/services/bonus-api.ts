import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Bonus, BonusCreated, BonusFilter, CreateBonusRequest, Employee } from '../models/bonus';
import { PagedResponse } from '../models/paged-response';

@Injectable({ providedIn: 'root' })
export class BonusApi {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/Bonus`;

  list(filter: BonusFilter, pageIndex: number) {
    let params = new HttpParams().set('pageIndex', pageIndex).set('pageSize', 10);
    for (const [key, value] of Object.entries(filter)) {
      if (value !== null && value !== '') params = params.set(key, String(value));
    }
    return this.http.get<PagedResponse<Bonus>>(this.url, { params });
  }
  employee(departmentCode: string, employeeNumber: number) {
    return this.http.get<Employee>(`${this.url}/employee/${encodeURIComponent(departmentCode)}/${employeeNumber}`);
  }
  create(request: CreateBonusRequest) {
    return this.http.post<BonusCreated>(this.url, request);
  }
  cancel(id: number) { return this.http.put<void>(`${this.url}/${id}/cancel`, {}); }
  delete(id: number) { return this.http.delete<void>(`${this.url}/${id}`); }
}
