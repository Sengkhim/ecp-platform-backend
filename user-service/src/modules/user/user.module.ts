import { Module } from '@nestjs/common';
import { UserService } from './user.service';
import { UserController } from './user.controller';
import { UserRoleModule } from '@/modules/user-role/user-role.module';
import { RoleModule } from '@/modules/role/role.module';
@Module({
    imports: [
        UserRoleModule,
        RoleModule
    ],
    controllers: [UserController],
    providers: [UserService],
    exports: [UserService]
})
export class UserModule {}
