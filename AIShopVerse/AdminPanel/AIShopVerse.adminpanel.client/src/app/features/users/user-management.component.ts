import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { UserDto } from '../../core/models';

@Component({
  selector: 'app-user-management',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="mb-3">
      <h2>User Management</h2>
    </div>

    <div class="table-responsive">
      <table class="table table-striped table-hover">
        <thead class="table-dark">
          <tr>
            <th>Name</th>
            <th>Email</th>
            <th>Phone</th>
            <th>Roles</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let user of users">
            <td>{{ user.fullName }}</td>
            <td>{{ user.email }}</td>
            <td>{{ user.phoneNumber }}</td>
            <td>
              <span *ngFor="let role of user.roles" class="badge bg-primary me-1">{{ role }}</span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div *ngIf="users.length === 0" class="text-center text-muted mt-4">
      <p>No users found.</p>
    </div>
  `
})
export class UserManagementComponent implements OnInit {
  users: UserDto[] = [];

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.http.post<any>(`${environment.apiEndpoint}user/GetAll`, {})
      .pipe()
      .subscribe({
        next: (r) => this.users = r.data || [],
        error: () => this.users = []
      });
  }
}
