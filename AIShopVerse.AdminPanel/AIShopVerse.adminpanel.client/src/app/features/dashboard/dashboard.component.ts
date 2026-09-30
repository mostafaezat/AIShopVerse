import { Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import { Chart, LineController, LineElement, PointElement, LinearScale, CategoryScale,
  DoughnutController, ArcElement, Tooltip, Legend, Filler
} from 'chart.js';
import { DashboardService } from '../../core/services/dashboard.service';
import { SignalRService } from '../../core/services/signalr.service';
import { LoadingComponent } from '../../shared/components/loading/loading.component';

Chart.register(
  LineController, LineElement, PointElement, LinearScale, CategoryScale,
  DoughnutController, ArcElement, Tooltip, Legend, Filler
);

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, LoadingComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
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

  getStatusBadgeClass(status: string): string {
    const map: { [key: string]: string } = {
      'Pending': 'bg-warning text-dark',
      'Paid': 'bg-primary',
      'Processing': 'bg-info',
      'Shipped': 'bg-secondary',
      'Delivered': 'bg-success',
      'Cancelled': 'bg-danger',
      'Refunded': 'bg-danger'
    };
    return map[status] || 'bg-secondary';
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