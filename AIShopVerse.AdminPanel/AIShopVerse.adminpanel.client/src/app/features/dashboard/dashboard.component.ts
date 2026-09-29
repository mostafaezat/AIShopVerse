import { Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import { Chart, LineController, LineElement, PointElement, LinearScale, CategoryScale,
  DoughnutController, ArcElement, Tooltip, Legend, Filler
} from 'chart.js';
import { DashboardService } from '../../core/services/dashboard.service';
import { SignalRService } from '../../core/services/signalr.service';

Chart.register(
  LineController, LineElement, PointElement, LinearScale, CategoryScale,
  DoughnutController, ArcElement, Tooltip, Legend, Filler
);

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="dashboard">
      <div class="head">
        <h2>Dashboard</h2>
        <div class="range">
          <label>Range:</label>
          <select [(ngModel)]="days" (ngModelChange)="reload()">
            <option [ngValue]="7">Last 7 days</option>
            <option [ngValue]="30">Last 30 days</option>
            <option [ngValue]="90">Last 90 days</option>
            <option [ngValue]="null">All time</option>
          </select>
        </div>
      </div>

      <div *ngIf="loading" class="text-muted">Loading dashboard...</div>
      <div *ngIf="!loading && loadError" class="alert alert-warning">
        Unable to load dashboard data. <button (click)="reload()">Retry</button>
      </div>

      <div class="kpis" *ngIf="!loading && !loadError">
        <div class="kpi"><span class="k-label">Total Revenue</span><span class="k-value">{{ data?.totalRevenue | currency }}</span></div>
        <div class="kpi"><span class="k-label">Total Orders</span><span class="k-value">{{ data?.totalOrders }}</span></div>
        <div class="kpi"><span class="k-label">Avg Order Value</span><span class="k-value">{{ data?.averageOrderValue | currency }}</span></div>
        <div class="kpi"><span class="k-label">Users</span><span class="k-value">{{ data?.totalUsers }}</span></div>
        <div class="kpi"><span class="k-label">Products</span><span class="k-value">{{ data?.totalProducts }}</span></div>
        <div class="kpi"><span class="k-label">Low Stock</span><span class="k-value">{{ data?.lowStockCount }}</span></div>
        <div class="kpi"><span class="k-label">Pending Reviews</span><span class="k-value">{{ data?.pendingReviewsCount }}</span></div>
      </div>

      <div class="charts">
        <div class="chart card">
          <h3>Revenue Over Time</h3>
          <div class="canvas-wrap"><canvas #revenueCanvas></canvas></div>
        </div>
        <div class="chart card">
          <h3>Orders by Status</h3>
          <div class="canvas-wrap"><canvas #statusCanvas></canvas></div>
        </div>
      </div>

      <div class="tables">
        <div class="card">
          <h3>Top Products</h3>
          <table>
            <thead><tr><th>Product</th><th>Units</th><th>Revenue</th></tr></thead>
            <tbody>
              <tr *ngFor="let p of data?.topProducts || []">
                <td>{{ p.productName }}</td><td>{{ p.unitsSold }}</td><td>{{ p.revenue | currency }}</td>
              </tr>
              <tr *ngIf="!data?.topProducts?.length"><td colspan="3">No sales yet.</td></tr>
            </tbody>
          </table>
        </div>
        <div class="card">
          <h3>Low Stock Alerts</h3>
          <table>
            <thead><tr><th>Product</th><th>SKU</th><th>Stock</th></tr></thead>
            <tbody>
              <tr *ngFor="let p of data?.lowStockProducts || []">
                <td>{{ p.nameEN }}</td><td>{{ p.sku }}</td><td>{{ p.stockQuantity }}</td>
              </tr>
              <tr *ngIf="!data?.lowStockProducts?.length"><td colspan="3">All stock levels are healthy.</td></tr>
            </tbody>
          </table>
        </div>
        <div class="card">
          <h3>Recent Orders</h3>
          <table>
            <thead><tr><th>Order #</th><th>Status</th><th>Total</th><th>Date</th></tr></thead>
            <tbody>
              <tr *ngFor="let o of data?.recentOrders || []">
                <td>{{ o.orderNumber }}</td><td>{{ o.status }}</td><td>{{ o.total | currency }}</td><td>{{ o.createdAt | date: 'short' }}</td>
              </tr>
              <tr *ngIf="!data?.recentOrders?.length"><td colspan="4">No orders.</td></tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .dashboard { padding: 1rem; }
    .head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; gap: 1rem; flex-wrap: wrap; }
    .range select { padding: 0.4rem; }
    .kpis { display: grid; grid-template-columns: repeat(auto-fit, minmax(140px, 1fr)); gap: 1rem; margin-bottom: 1.5rem; }
    .kpi { background: #fff; border: 1px solid #e0e0e0; border-radius: 8px; padding: 1rem; display: flex; flex-direction: column; }
    .k-label { color: #888; font-size: 0.8rem; margin-bottom: 0.3rem; }
    .k-value { font-size: 1.4rem; font-weight: 700; }
    .charts { display: grid; grid-template-columns: 2fr 1fr; gap: 1rem; margin-bottom: 1.5rem; }
    .canvas-wrap { position: relative; height: 320px; }
    .tables { display: grid; grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); gap: 1rem; }
    .card { background: #fff; border: 1px solid #e0e0e0; border-radius: 8px; padding: 1rem; }
    .card h3 { margin-top: 0; }
    table { width: 100%; border-collapse: collapse; font-size: 0.9rem; }
    th, td { text-align: left; padding: 0.5rem 0.4rem; border-bottom: 1px solid #eee; }
    th { color: #888; font-weight: 600; }
    @media (max-width: 900px) { .charts { grid-template-columns: 1fr; } }
  `]
})
export class DashboardComponent implements OnInit, OnDestroy {
  data: any;
  days: number | null = 30;
  loading = true;
  loadError = false;

  @ViewChild('revenueCanvas') revenueCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('statusCanvas') statusCanvas!: ElementRef<HTMLCanvasElement>;

  private revenueChart: Chart | null = null;
  private statusChart: Chart | null = null;
  private destroy$ = new Subject<void>();

  constructor(private dashboardService: DashboardService, private signalR: SignalRService) {}

  ngOnInit() {
    this.load();
    this.signalR.newOrder$.pipe(takeUntil(this.destroy$)).subscribe(() => this.load());
    this.signalR.lowStock$.pipe(takeUntil(this.destroy$)).subscribe(() => this.load());
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
    if (this.revenueChart) this.revenueChart.destroy();
    if (this.statusChart) this.statusChart.destroy();
  }

  reload() { this.load(); }

  private load() {
    this.loading = true;
    this.loadError = false;
    this.dashboardService.getAnalytics(this.days ?? undefined).subscribe({
      next: res => {
        this.data = res.data;
        this.loading = false;
        this.renderCharts();
      },
      error: () => {
        this.loading = false;
        this.loadError = true;
      }
    });
  }

  private renderCharts() {
    const rev = this.data?.revenueOverTime || [];
    const status = this.data?.ordersByStatus || [];

    if (this.revenueChart) { this.revenueChart.destroy(); this.revenueChart = null; }
    if (this.statusChart) { this.statusChart.destroy(); this.statusChart = null; }

    if (this.revenueCanvas?.nativeElement && rev.length) {
      this.revenueChart = new Chart(this.revenueCanvas.nativeElement, {
        type: 'line',
        data: {
          labels: rev.map((p: any) => new Date(p.date).toLocaleDateString()),
          datasets: [{ label: 'Revenue', data: rev.map((p: any) => p.amount), borderColor: '#3b82f6', backgroundColor: 'rgba(59,130,246,.15)', fill: true, tension: 0.3 }]
        },
        options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { display: false } } }
      });
    }

    if (this.statusCanvas?.nativeElement && status.length) {
      this.statusChart = new Chart(this.statusCanvas.nativeElement, {
        type: 'doughnut',
        data: {
          labels: status.map((s: any) => s.status),
          datasets: [{ data: status.map((s: any) => s.count) }]
        },
        options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { display: true, position: 'right' } } }
      });
    }
  }
}
