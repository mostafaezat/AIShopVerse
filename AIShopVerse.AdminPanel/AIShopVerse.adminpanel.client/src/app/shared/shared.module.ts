import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import { LoadingComponent } from './components/loading/loading.component';
import { EmptyStateComponent } from './components/empty-state/empty-state.component';
import { ConfirmDialogComponent } from './components/confirm-dialog/confirm-dialog.component';
import { ModalComponent } from './components/modal/modal.component';

const SHARED_COMPONENTS = [
  LoadingComponent,
  EmptyStateComponent,
  ConfirmDialogComponent,
  ModalComponent
];

@NgModule({
  declarations: [],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    ...SHARED_COMPONENTS
  ],
  exports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    ...SHARED_COMPONENTS
  ]
})
export class SharedModule { }