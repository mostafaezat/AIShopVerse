import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { CategoryService } from '../../../core/services/category.service';
import { SweetAlertService } from '../../../shared/services/sweet-alert.service';
import { LoadingComponent } from '../../../shared/components/loading/loading.component';

@Component({
  selector: 'app-category-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LoadingComponent],
  templateUrl: './category-form.component.html',
  styleUrl: './category-form.component.scss'
})
export class CategoryFormComponent implements OnInit {
  form: FormGroup;
  isEdit = false;
  categoryId: string | null = null;
  saving = false;
  submitted = false;
  categories: any[] = [];

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private categoryService: CategoryService,
    private sweetAlert: SweetAlertService
  ) {
    this.form = this.fb.group({
      nameEN: ['', [Validators.required, Validators.maxLength(100)]],
      nameAR: ['', [Validators.required, Validators.maxLength(100)]],
      description: ['', Validators.maxLength(500)],
      imageUrl: ['', Validators.pattern('^https?://.+')],
      parentId: [null],
      displayOrder: [0, [Validators.min(0)]],
      isActive: [true]
    });
  }

  ngOnInit() {
    this.loadCategoriesForParent();

    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.isEdit = true;
        this.categoryId = id;
        this.loadCategory(id);
      }
    });
  }

  loadCategoriesForParent() {
    this.categoryService.getAll().subscribe({
      next: res => {
        this.categories = res.data || [];
      }
    });
  }

  loadCategory(id: string) {
    this.categoryService.getById(id).subscribe({
      next: res => {
        const cat = res.data;
        this.form.patchValue({
          nameEN: cat.nameEN,
          nameAR: cat.nameAR,
          description: cat.description || '',
          imageUrl: cat.imageUrl || '',
          parentId: cat.parentId || null,
          displayOrder: cat.displayOrder || 0,
          isActive: cat.isActive
        });
      },
      error: (err: any) => {
        this.sweetAlert.toastError(err?.error?.message || 'Failed to load category');
        this.router.navigate(['/categories']);
      }
    });
  }

  get f() { return this.form.controls; }

  onSubmit() {
    this.submitted = true;
    if (this.form.invalid) return;

    const payload = {
      nameEN: this.f['nameEN'].value.trim(),
      nameAR: this.f['nameAR'].value.trim(),
      description: this.f['description'].value?.trim() || null,
      imageUrl: this.f['imageUrl'].value?.trim() || null,
      parentId: this.f['parentId'].value || null,
      displayOrder: this.f['displayOrder'].value || 0,
      isActive: this.f['isActive'].value
    };

    this.saving = true;
    const call = this.isEdit && this.categoryId
      ? this.categoryService.update({ ...payload, id: this.categoryId })
      : this.categoryService.add(payload);

    call.subscribe({
      next: () => {
        this.saving = false;
        this.sweetAlert.toastSuccess(this.isEdit ? 'Category updated.' : 'Category created.');
        this.router.navigate(['/categories']);
      },
      error: (err: any) => {
        this.saving = false;
        this.sweetAlert.toastError(err?.error?.message || (this.isEdit ? 'Failed to update category' : 'Failed to create category'));
      }
    });
  }
}