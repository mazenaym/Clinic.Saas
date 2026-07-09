import { Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Patient } from '../../core/models';
import { CfAvatarComponent, CfBadgeComponent, CfEmptyStateComponent } from '../../shared/ui';

@Component({
  selector: 'app-patients-table',
  standalone: true,
  imports: [RouterLink, CfAvatarComponent, CfBadgeComponent, CfEmptyStateComponent],
  template: `
    @if (patients.length) {
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>المريض</th>
              <th>الكود</th>
              <th>الهاتف</th>
              <th>العمر</th>
              <th>ملاحظات طبية</th>
              <th>إجراءات</th>
            </tr>
          </thead>
          <tbody>
            @for (patient of patients; track patient.id) {
              <tr>
                <td>
                  <div class="patient-cell">
                    <cf-avatar [name]="patient.fullName" />
                    <div>
                      <strong>{{ patient.fullName }}</strong>
                      <span>{{ patient.gender || 'غير محدد' }}</span>
                    </div>
                  </div>
                </td>
                <td>{{ patient.patientCode }}</td>
                <td>{{ patient.phoneNumber }}</td>
                <td>{{ patient.age || '-' }}</td>
                <td>
                  @if (patient.drugAllergies || patient.chronicDiseases) {
                    <cf-badge [label]="patient.drugAllergies || patient.chronicDiseases || ''" variant="warning" />
                  } @else {
                    <cf-badge label="لا توجد تنبيهات" variant="neutral" />
                  }
                </td>
                <td>
                  <div class="actions">
                    <a class="button-link secondary-link" [routerLink]="['/patients', patient.id, 'chart']">ملف المريض</a>
                    <button type="button" class="secondary" (click)="edit.emit(patient)">تعديل</button>
                    <button type="button" class="danger" (click)="remove.emit(patient)">حذف</button>
                  </div>
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    } @else {
      <cf-empty-state
        title="لا توجد بيانات مرضى"
        description="ابدأ بإضافة أول مريض ليظهر هنا مع ملفه الطبي وملاحظاته."
      />
    }
  `,
  styles: [`
    .patient-cell {
      display: flex;
      align-items: center;
      gap: var(--cf-space-3);
    }

    .patient-cell strong,
    .patient-cell span {
      display: block;
    }

    .patient-cell span {
      margin-top: var(--cf-space-1);
      color: var(--cf-color-muted);
      font-size: var(--cf-font-13);
    }
  `],
})
export class PatientsTableComponent {
  @Input() patients: Patient[] = [];
  @Output() edit = new EventEmitter<Patient>();
  @Output() remove = new EventEmitter<Patient>();
}
