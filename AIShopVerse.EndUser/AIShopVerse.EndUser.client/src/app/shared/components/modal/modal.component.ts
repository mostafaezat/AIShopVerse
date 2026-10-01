import { Component, EventEmitter, Input, Output, ViewChild, ElementRef, AfterViewInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-modal',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './modal.component.html',
  styleUrl: './modal.component.scss'
})
export class ModalComponent implements AfterViewInit, OnDestroy {
  private translate = inject(TranslateService);

  @Input() modalId = 'app-modal';
  @Input() title = '';
  @Input() size: 'modal-sm' | 'modal-lg' | 'modal-xl' | 'modal-fullscreen' = 'modal-lg';
  @Input() scrollable = true;
  @Input() showClose = true;
  @Input() showFooter = true;
  @Input() backdrop: 'static' | true = 'static';
  @Input() keyboard = true;

  @Output() onOpen = new EventEmitter<void>();
  @Output() onClose = new EventEmitter<void>();
  @Output() onConfirm = new EventEmitter<void>();
  @Output() onCancel = new EventEmitter<void>();

  @ViewChild('modalElement') modalElement?: ElementRef<HTMLDivElement>;

  private bsModal?: any;

  ngAfterViewInit(): void {
    // Bootstrap Modal will be initialized when needed
  }

  ngOnDestroy(): void {
    this.hide();
  }

  show(): void {
    if (this.modalElement?.nativeElement && (window as any).bootstrap) {
      this.bsModal = new (window as any).bootstrap.Modal(this.modalElement.nativeElement, {
        backdrop: this.backdrop,
        keyboard: this.keyboard
      });

      this.modalElement.nativeElement.addEventListener('hidden.bs.modal', () => {
        this.onClose.emit();
      });

      this.bsModal.show();
      this.onOpen.emit();
    }
  }

  hide(): void {
    if (this.bsModal) {
      this.bsModal.hide();
      this.bsModal.dispose();
      this.bsModal = undefined;
    }
  }

  confirm(): void {
    this.onConfirm.emit();
    this.hide();
  }

  cancel(): void {
    this.onCancel.emit();
    this.hide();
  }
}