import { Component, Input, ContentChild, TemplateRef } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './empty-state.component.html',
  styleUrl: './empty-state.component.scss'
})
export class EmptyStateComponent {
  @Input() icon = 'bi-inbox';
  @Input() title = 'No Data';
  @Input() message = 'There are no items to display.';
  @ContentChild('action') actionTemplate?: TemplateRef<unknown>;
}