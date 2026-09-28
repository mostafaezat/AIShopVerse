import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { Subscription, timer, Subject, of } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { AuthService } from './core/services/auth.service';
import { CategoryService, CategoryTreeDto } from './core/services/category.service';
import { NotificationService, NotificationDto } from './core/services/notification.service';
import { SignalRService } from './core/services/signalr.service';
import { SearchService, SearchSuggestion } from './core/services/search.service';
import { ProductFilterService } from './core/services/product-filter.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <header>
      <nav>
        <a routerLink="/">AIShopVerse</a>
        <a routerLink="/products">Products</a>

        <div class="search-wrap">
          <input type="text" class="search-input" placeholder="Search..."
                 (input)="onSearchInput($event)" (keydown.enter)="submitSearch()"
                 (focus)="onFocus()" (blur)="closeSuggestions()">
          <div class="search-dropdown" *ngIf="suggestions.length && showSuggestions">
            <a class="search-item" *ngFor="let s of suggestions"
               (mousedown)="goToProduct(s)">
              <img [src]="s.imageUrl || 'https://via.placeholder.com/40x40?text=No+Image'"
                   class="search-thumb" alt="">
              <div class="search-item-body">
                <div class="search-name">{{ s.nameEN }}</div>
                <div class="search-price">{{ (s.discountPrice ?? s.price) | currency }}</div>
              </div>
            </a>
            <a class="search-more" (mousedown)="submitSearch()">See all results for "{{ searchTerm }}"</a>
          </div>
        </div>

        <div class="dropdown" *ngIf="categories.length">
          <button class="dropbtn">Categories ▾</button>
          <div class="dropdown-content">
            <a *ngFor="let c of flatten(categories)" [routerLink]="['/products']"
               (click)="selectCategory(c)">{{ c.nameEN }}</a>
          </div>
        </div>

        <a routerLink="/cart" *ngIf="auth.isLoggedIn()">Cart</a>
        <a routerLink="/orders" *ngIf="auth.isLoggedIn()">Orders</a>
        <a routerLink="/wishlist" *ngIf="auth.isLoggedIn()">Wishlist</a>

        <div class="notif-wrap" *ngIf="auth.isLoggedIn()">
          <button class="notif-bell" (click)="toggleNotifications()">Notifications
            <span class="notif-badge" *ngIf="unreadCount > 0">{{ unreadCount }}</span>
          </button>
          <div class="notif-panel" *ngIf="showNotifications" (click)="$event.stopPropagation()">
            <div class="notif-header">
              <span>Notifications</span>
              <button class="notif-markall" (click)="markAllRead()">Mark all read</button>
            </div>
            <div class="notif-empty" *ngIf="!notifications.length">No notifications</div>
            <a class="notif-item" [class.unread]="!n.isRead"
               *ngFor="let n of notifications" (click)="openNotification(n)">
              <div class="notif-title">{{ n.title }}</div>
              <div class="notif-msg" *ngIf="n.message">{{ n.message }}</div>
              <div class="notif-time">{{ n.createdAt | date: 'short' }}</div>
            </a>
          </div>
        </div>

        <a routerLink="/login" *ngIf="!auth.isLoggedIn()">Login</a>
        <a routerLink="/register" *ngIf="!auth.isLoggedIn()">Register</a>
        <button *ngIf="auth.isLoggedIn()" (click)="auth.logout()">Logout</button>
      </nav>
    </header>
    <main><router-outlet></router-outlet></main>
    <footer><p>&copy; AIShopVerse</p></footer>
  `,
  styles: [`
    nav { display: flex; gap: 1rem; padding: 1rem; background: #333; color: white; align-items: center; }
    nav a { color: white; text-decoration: none; }
    main { min-height: 80vh; padding: 2rem; }
    footer { text-align: center; padding: 1rem; background: #f5f5f5; }
    .dropdown { position: relative; display: inline-block; }
    .dropbtn { background: none; border: none; color: white; cursor: pointer; font-size: 1rem; }
    .dropdown-content { display: none; position: absolute; background: #fff; min-width: 220px;
      box-shadow: 0 8px 16px rgba(0,0,0,.15); z-index: 1000; max-height: 60vh; overflow: auto; }
    .dropdown:hover .dropdown-content { display: block; }
    .dropdown-content a { color: #333; padding: 8px 16px; display: block; }
    .dropdown-content a:hover { background: #f5f5f5; }
    .notif-wrap { position: relative; display: inline-block; }
    .notif-bell { background: none; border: none; color: white; cursor: pointer; font-size: 1rem; position: relative; }
    .notif-badge { position: absolute; top: -8px; right: -12px; background: #dc3545; color: #fff;
      border-radius: 50%; padding: 0 5px; font-size: 0.7rem; }
    .notif-panel { position: absolute; right: 0; top: 28px; background: #fff; color: #333; min-width: 320px;
      max-height: 60vh; overflow: auto; box-shadow: 0 8px 16px rgba(0,0,0,.2); z-index: 2000; }
    .notif-header { display: flex; justify-content: space-between; align-items: center; padding: 8px 12px;
      border-bottom: 1px solid #ddd; }
    .notif-markall { background: none; border: none; color: #0d6efd; cursor: pointer; font-size: 0.8rem; }
    .notif-empty { padding: 12px; color: #888; }
    .notif-item { display: block; padding: 8px 12px; border-bottom: 1px solid #eee; color: #333; text-decoration: none; }
    .notif-item:hover { background: #f5f5f5; }
    .notif-item.unread { background: #eef4ff; }
    .notif-title { font-weight: 600; }
    .notif-msg { font-size: 0.85rem; }
    .notif-time { font-size: 0.75rem; color: #888; }
    .search-wrap { position: relative; flex: 1; max-width: 420px; }
    .search-input { width: 100%; padding: 8px 12px; border-radius: 20px; border: none; font-size: 0.95rem; }
    .search-dropdown { position: absolute; top: 40px; left: 0; right: 0; background: #fff; color: #333;
      box-shadow: 0 8px 16px rgba(0,0,0,.2); z-index: 3000; border-radius: 8px; overflow: hidden; }
    .search-item { display: flex; align-items: center; gap: 10px; padding: 8px 12px;
      color: #333; text-decoration: none; cursor: pointer; }
    .search-item:hover { background: #f5f5f5; }
    .search-thumb { width: 40px; height: 40px; object-fit: cover; border-radius: 4px; }
    .search-item-body { flex: 1; min-width: 0; }
    .search-name { white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .search-price { font-size: 0.8rem; color: #0d6efd; }
    .search-more { display: block; padding: 8px 12px; color: #0d6efd; text-decoration: none;
      cursor: pointer; border-top: 1px solid #eee; font-size: 0.85rem; }
    .search-more:hover { background: #f5f5f5; }
  `]
})
export class AppComponent implements OnInit, OnDestroy {
  categories: CategoryTreeDto[] = [];
  notifications: NotificationDto[] = [];
  unreadCount = 0;
  showNotifications = false;
  searchTerm = '';
  suggestions: SearchSuggestion[] = [];
  showSuggestions = false;
  private pollSub: Subscription | null = null;
  private authSub: Subscription | null = null;
  private signalSub: Subscription | null = null;
  private infoSub: Subscription | null = null;
  private searchSub: Subscription | null = null;
  private searchSubject = new Subject<string>();

  constructor(
    public auth: AuthService,
    private categoryService: CategoryService,
    private router: Router,
    private notificationService: NotificationService,
    private signalR: SignalRService,
    private searchService: SearchService,
    private filterService: ProductFilterService
  ) {}

  ngOnInit() {
    this.categoryService.getCategoryTree().subscribe(tree => this.categories = tree || []);

    this.searchSub = this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(term => {
        const t = term.trim();
        if (t.length < 2) return of([] as SearchSuggestion[]);
        return this.searchService.suggest(t);
      })
    ).subscribe({
      next: items => {
        this.suggestions = items || [];
        this.showSuggestions = this.suggestions.length > 0;
      },
      error: () => {
        this.suggestions = [];
        this.showSuggestions = false;
      }
    });

    this.authSub = this.auth.currentUser$.subscribe(loggedIn => {
      if (loggedIn) {
        this.refreshNotifications();
        this.startPolling();
      } else {
        this.stopPolling();
        this.notifications = [];
        this.unreadCount = 0;
      }
    });

    this.signalSub = this.signalR.orderStatus$.subscribe(() => {
      if (this.auth.isLoggedIn() && this.showNotifications) {
        this.refreshNotifications();
      } else if (this.auth.isLoggedIn()) {
        this.loadUnreadCount();
      }
    });

    this.infoSub = this.signalR.info$.subscribe(() => {
      if (this.auth.isLoggedIn()) this.refreshNotifications();
    });
  }

  ngOnDestroy() {
    this.stopPolling();
    if (this.authSub) this.authSub.unsubscribe();
    if (this.signalSub) this.signalSub.unsubscribe();
    if (this.infoSub) this.infoSub.unsubscribe();
    if (this.searchSub) this.searchSub.unsubscribe();
  }

  private startPolling() {
    if (this.pollSub) return;
    this.pollSub = timer(30000, 30000).subscribe(() => {
      if (this.auth.isLoggedIn()) this.refreshNotifications();
    });
  }

  private stopPolling() {
    if (this.pollSub) { this.pollSub.unsubscribe(); this.pollSub = null; }
  }

  refreshNotifications() {
    this.loadUnreadCount();
    if (this.showNotifications) this.loadNotifications();
  }

  loadUnreadCount() {
    this.notificationService.getUnreadCount().subscribe({
      next: c => this.unreadCount = c,
      error: () => {}
    });
  }

  loadNotifications() {
    this.notificationService.getAll(false, 20).subscribe({
      next: items => this.notifications = items,
      error: () => {}
    });
  }

  toggleNotifications() {
    this.showNotifications = !this.showNotifications;
    if (this.showNotifications) this.loadNotifications();
  }

  markAllRead() {
    this.notificationService.markAllRead().subscribe({
      next: () => {
        this.notifications.forEach(n => n.isRead = true);
        this.unreadCount = 0;
      },
      error: () => {}
    });
  }

  openNotification(n: NotificationDto) {
    this.showNotifications = false;
    if (!n.isRead) {
      this.notificationService.markRead(n.id).subscribe({
        next: () => {
          n.isRead = true;
          this.loadUnreadCount();
        },
        error: () => {}
      });
    }
    if (n.orderId) {
      this.router.navigate(['/orders', n.orderId]);
    }
  }

  flatten(nodes: CategoryTreeDto[]): CategoryTreeDto[] {
    let flat: CategoryTreeDto[] = [];
    for (const n of nodes) {
      flat.push(n);
      if (n.children?.length) flat = flat.concat(this.flatten(n.children));
    }
    return flat;
  }

  selectCategory(c: CategoryTreeDto) {
    this.router.navigate(['/products'], { queryParams: { category: c.id } });
  }

  onSearchInput(event: Event) {
    this.searchTerm = (event.target as HTMLInputElement).value;
    this.searchSubject.next(this.searchTerm);
  }

  onFocus() {
    if (this.searchTerm.trim().length >= 2) {
      this.searchSubject.next(this.searchTerm);
    }
  }

  submitSearch() {
    const t = this.searchTerm.trim();
    this.showSuggestions = false;
    if (!t) return;
    this.filterService.updateFilter({ searchTerm: t });
    this.router.navigate(['/products'], { queryParams: { search: t } });
  }

  goToProduct(s: SearchSuggestion) {
    this.showSuggestions = false;
    this.router.navigate(['/products', s.id]);
  }

  closeSuggestions() {
    setTimeout(() => this.showSuggestions = false, 150);
  }
}
